namespace AddressRegistry.Api.Extract.Extracts
{
    using Be.Vlaanderen.Basisregisters.Api;
    using Be.Vlaanderen.Basisregisters.Api.Exceptions;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Projections.Extract;
    using Responses;
    using Swashbuckle.AspNetCore.Filters;
    using System.Threading;
    using System.Threading.Tasks;
    using Be.Vlaanderen.Basisregisters.Api.Extract;
    using Consumer.Read.Municipality;
    using Consumer.Read.StreetName;
    using Infrastructure.FeatureToggles;
    using Microsoft.Extensions.Configuration;
    using Projections.Syndication;
    using ProblemDetails = Be.Vlaanderen.Basisregisters.BasicApiProblem.ProblemDetails;

    [ApiVersion("1.0")]
    [AdvertiseApiVersions("1.0")]
    [ApiRoute("extract")]
    [ApiExplorerSettings(GroupName = "Extract")]
    public class ExtractController : ApiController
    {
        /// <summary>
        /// Vraag een dump van het volledige register op.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="useExtractV2Toggle"></param>
        /// <param name="syndicationContext"></param>
        /// <param name="municipalityConsumerContext"></param>
        /// <param name="streetNameConsumerContext"></param>
        /// <param name="cancellationToken"></param>
        /// <response code="200">Als adresregister kan gedownload worden.</response>
        /// <response code="500">Als er een interne fout is opgetreden.</response>
        [HttpGet]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(AddressRegistryResponseExample))]
        [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorResponseExamples))]
        public async Task<IActionResult> Get(
            [FromServices] ExtractContext context,
            [FromServices] UseExtractV2Toggle useExtractV2Toggle,
            [FromServices] SyndicationContext syndicationContext,
            [FromServices] MunicipalityConsumerContext municipalityConsumerContext,
            [FromServices] StreetNameConsumerContext streetNameConsumerContext,
            CancellationToken cancellationToken = default)
        {
            if (useExtractV2Toggle.FeatureEnabled)
            {
                return new IsolationExtractArchive(ExtractFileNames.GetAddressZip(), context)
                    {
                        AddressRegistryExtractBuilder.CreateAddressFilesV2(context, streetNameConsumerContext, municipalityConsumerContext),
                        AddressCrabHouseNumberIdExtractBuilder.CreateAddressCrabHouseNumberIdFile(context),
                        AddressCrabSubaddressIdExtractBuilder.CreateAddressSubaddressIdFile(context)
                    }
                    .CreateFileCallbackResult(cancellationToken);
            }

            return new IsolationExtractArchive(ExtractFileNames.GetAddressZip(), context)
                {
                    AddressRegistryExtractBuilder.CreateAddressFiles(context, syndicationContext),
                    AddressCrabHouseNumberIdExtractBuilder.CreateAddressCrabHouseNumberIdFile(context),
                    AddressCrabSubaddressIdExtractBuilder.CreateAddressSubaddressIdFile(context)
                }
                .CreateFileCallbackResult(cancellationToken);
        }

        /// <summary>
        /// Vraag een dump van alle adreskoppelingen op.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="syndicationContext"></param>
        /// <param name="cancellationToken"></param>
        /// <response code="200">Als adreskoppelingen kan gedownload worden.</response>
        /// <response code="500">Als er een interne fout is opgetreden.</response>
        [HttpGet("addresslinks")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [SwaggerResponseExample(StatusCodes.Status200OK, typeof(AddressRegistryResponseExample))]
        [SwaggerResponseExample(StatusCodes.Status500InternalServerError, typeof(InternalServerErrorResponseExamples))]
        public async Task<IActionResult> GetAddressLinks(
            [FromServices] IConfiguration configuration,
            [FromServices] SyndicationContext syndicationContext,
            CancellationToken cancellationToken = default)
        {
            var extractBuilder = new LinkedAddressExtractBuilder(syndicationContext, configuration.GetConnectionString("SyndicationProjections"));

            return new ExtractArchive(ExtractFileNames.GetAddressLinksZip())
                {
                    extractBuilder.CreateLinkedBuildingUnitAddressFiles(),
                    await extractBuilder.CreateLinkedParcelAddressFiles(cancellationToken)
                }
                .CreateFileCallbackResult(cancellationToken);
        }
    }
}
