namespace AddressRegistry.Consumer.Infrastructure
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Autofac;
    using Autofac.Extensions.DependencyInjection;
    using Be.Vlaanderen.Basisregisters.Aws.DistributedMutex;
    using Be.Vlaanderen.Basisregisters.EventHandling;
    using Be.Vlaanderen.Basisregisters.MessageHandling.Kafka.Simple;
    using Be.Vlaanderen.Basisregisters.Projector.ConnectedProjections;
    using Be.Vlaanderen.Basisregisters.Projector.Modules;
    using Confluent.Kafka;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Modules;
    using Serilog;
    using SqlStreamStore;
    using SqlStreamStore.Streams;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    public class Program
    {
        private static readonly AutoResetEvent Closing = new AutoResetEvent(false);
        private static readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        protected Program()
        { }

        public static async Task Main(string[] args)
        {
            var cancellationToken = CancellationTokenSource.Token;

            cancellationToken.Register(() => Closing.Set());
            Console.CancelKeyPress += (sender, eventArgs) => CancellationTokenSource.Cancel();

            AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
                Log.Debug(
                    eventArgs.Exception,
                    "FirstChanceException event raised in {AppDomain}.",
                    AppDomain.CurrentDomain.FriendlyName);

            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
                Log.Fatal((Exception)eventArgs.ExceptionObject, "Encountered a fatal exception, exiting program.");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile($"appsettings.{Environment.MachineName.ToLowerInvariant()}.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build();

            var container = ConfigureServices(configuration);

            Log.Information("Starting AddressRegistry.Consumer");

            try
            {
                await DistributedLock<Program>.RunAsync(
                    async () =>
                    {
                        try
                        {
                            async Task<Offset?> GetOffset(IServiceProvider serviceProvider, ILogger logger, string topic)
                            {
                                if (long.TryParse(configuration["StreetNameTopicOffset"], out var offset))
                                {
                                    var streamStore = serviceProvider.GetRequiredService<IStreamStore>();
                                    var lastMessagePage = await streamStore.ReadAllBackwards(StreamVersion.End, 1,
                                        false, cancellationToken);

                                    var lastMessage = lastMessagePage.Messages.FirstOrDefault();
                                    if (lastMessagePage.Messages.Any() && lastMessage.StreamId.StartsWith("streetname-",
                                            StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        throw new InvalidOperationException(
                                            "Cannot start migration from offset, because migration is already running. Remove offset to continue.");
                                    }

                                    logger.LogInformation($"Starting {topic} from offset {offset}.");
                                    return new Offset(offset);
                                }

                                logger.LogInformation($"Continuing {topic} from last offset.");
                                return null;
                            }

                            var loggerFactory = container.GetRequiredService<ILoggerFactory>();

                            await MigrationsHelper.RunAsync(configuration.GetConnectionString("ConsumerAdmin"),
                                loggerFactory, cancellationToken);

                            var bootstrapServers = configuration["Kafka:BootstrapServers"];
                            var kafkaOptions = new KafkaOptions(bootstrapServers, configuration["Kafka:SaslUserName"],
                                configuration["Kafka:SaslPassword"],
                                EventsJsonSerializerSettingsProvider.CreateSerializerSettings());

                            var topic = $"{configuration["StreetNameTopic"]}" ??
                                        throw new ArgumentException("Configuration has no MunicipalityTopic.");
                            var consumerGroupSuffix = configuration["StreetNameConsumerGroupSuffix"];

                            var actualContainer = container.GetRequiredService<ILifetimeScope>();

                            var kafkaOffset = await GetOffset(container, loggerFactory.CreateLogger<Program>(), topic);

                            var consumer = new Consumer(actualContainer, loggerFactory, kafkaOptions, topic, consumerGroupSuffix, kafkaOffset);
                            var consumerTask = consumer.Start(cancellationToken);

                            Log.Information("The kafka consumer was started");
                            
                            var projectorRunner = new ProjectorRunner(actualContainer.Resolve<IConnectedProjectionsManager>(), actualContainer.Resolve<ILoggerFactory>());
                            var projectorTask = projectorRunner.Start(cancellationToken);

                            Log.Information("The projection consumer was started");

                            await Task.WhenAny(consumerTask, projectorTask);

                            CancellationTokenSource.Cancel();

                            Log.Error($"Consumer task stopped with status: {consumerTask.Status}");
                            Log.Error($"Projector task stopped with status: {projectorTask.Status}");

                            Log.Error("The consumer was terminated");
                        }
                        catch (Exception e)
                        {
                            Log.Fatal(e, "Encountered a fatal exception, exiting program.");
                            throw;
                        }
                    },
                    DistributedLockOptions.LoadFromConfiguration(configuration),
                    container.GetService<ILogger<Program>>()!);
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Encountered a fatal exception, exiting program.");
                Log.CloseAndFlush();

                // Allow some time for flushing before shutdown.
                await Task.Delay(1000, default);
                throw;
            }

            Log.Information("Stopping...");
            Closing.Close();
        }

        private static IServiceProvider ConfigureServices(IConfiguration configuration)
        {
            var services = new ServiceCollection();
            var builder = new ContainerBuilder();

            builder.RegisterModule(new LoggingModule(configuration, services));

            var tempProvider = services.BuildServiceProvider();
            var loggerFactory = tempProvider.GetRequiredService<ILoggerFactory>();

            builder.RegisterModule(new ApiModule(configuration, services, loggerFactory));
            builder.RegisterModule(new ConsumerModule(configuration, services, loggerFactory));
            builder.RegisterModule(new ProjectorModule(configuration));

            builder.Populate(services);

            return new AutofacServiceProvider(builder.Build());
        }
    }
}
