namespace AddressRegistry.Api.BackOffice.Handlers.Sqs.Lambda.Handlers
{
    using System.Threading;
    using System.Threading.Tasks;
    using Abstractions.Requests;
    using Be.Vlaanderen.Basisregisters.CommandHandling;
    using Be.Vlaanderen.Basisregisters.CommandHandling.Idempotency;
    using StreetName;
    using StreetName.Commands;
    using TicketingService.Abstractions;

    public class SqsAddressRetireHandler : SqsLambdaHandler<SqsAddressRetireRequest>
    {
        private readonly IStreetNames _streetNames;
        private readonly IdempotencyContext _idempotencyContext;

        public SqsAddressRetireHandler(
            ITicketing ticketing,
            ITicketingUrl ticketingUrl,
            ICommandHandlerResolver bus,
            IStreetNames streetNames,
            IdempotencyContext idempotencyContext)
            : base(ticketing, ticketingUrl, bus)
        {
            _streetNames = streetNames;
            _idempotencyContext = idempotencyContext;
        }

        protected override async Task<string> Handle2(SqsAddressRetireRequest request, CancellationToken cancellationToken)
        {
            var streetNamePersistentLocalId = new StreetNamePersistentLocalId(int.Parse(request.MessageGroupId));
            var addressPersistentLocalId = new AddressPersistentLocalId(request.PersistentLocalId);

            var cmd = new RetireAddress(
                streetNamePersistentLocalId,
                addressPersistentLocalId,
                CreateFakeProvenance());

            await IdempotentCommandHandlerDispatch(
                _idempotencyContext,
                cmd.CreateCommandId(),
                cmd,
                request.Metadata,
                cancellationToken);

            var etag = await GetHash(
                _streetNames,
                streetNamePersistentLocalId,
                addressPersistentLocalId,
                cancellationToken);

            return etag;
        }
    }
}
