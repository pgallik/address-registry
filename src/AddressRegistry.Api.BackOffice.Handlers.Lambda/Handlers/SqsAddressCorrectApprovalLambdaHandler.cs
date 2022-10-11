namespace AddressRegistry.Api.BackOffice.Handlers.Lambda.Handlers
{
    using System.Threading;
    using System.Threading.Tasks;
    using Abstractions;
    using Abstractions.Exceptions;
    using Abstractions.Responses;
    using Abstractions.Validation;
    using AddressRegistry.Infrastructure;
    using Be.Vlaanderen.Basisregisters.AggregateSource;
    using Microsoft.Extensions.Configuration;
    using Requests;
    using StreetName;
    using StreetName.Exceptions;
    using TicketingService.Abstractions;

    public sealed class SqsAddressCorrectApprovalLambdaHandler : SqsLambdaHandler<SqsLambdaAddressCorrectApprovalRequest>
    {
        public SqsAddressCorrectApprovalLambdaHandler(
             IConfiguration configuration,
             ICustomRetryPolicy retryPolicy,
             ITicketing ticketing,
             IStreetNames streetNames,
             IIdempotentCommandHandler idempotentCommandHandler)
             : base(
                 configuration,
                 retryPolicy,
                 streetNames,
                 ticketing,
                 idempotentCommandHandler)
        { }

        protected override async Task<ETagResponse> InnerHandle(SqsLambdaAddressCorrectApprovalRequest request, CancellationToken cancellationToken)
        {
            var addressPersistentLocalId = new AddressPersistentLocalId(request.Request.PersistentLocalId);
            var cmd = request.ToCommand();

            try
            {
                await IdempotentCommandHandler.Dispatch(
                    cmd.CreateCommandId(),
                    cmd,
                    request.Metadata,
                    cancellationToken);
            }
            catch (IdempotencyException)
            {
                // Idempotent: Do Nothing return last etag
            }

            var lastHash = await GetHash(request.StreetNamePersistentLocalId, addressPersistentLocalId, cancellationToken);
            return new ETagResponse(string.Format(DetailUrlFormat, addressPersistentLocalId), lastHash);
        }

        protected override TicketError? MapDomainException(DomainException exception, SqsLambdaAddressCorrectApprovalRequest request)
        {
            return exception switch
            {
                StreetNameIsRemovedException => new TicketError(
                    ValidationErrors.Common.StreetNameInvalid.Message(request.StreetNamePersistentLocalId),
                    ValidationErrors.Common.StreetNameInvalid.Code),
                StreetNameHasInvalidStatusException => new TicketError(
                    ValidationErrors.Common.StreetNameStatusInvalidForCorrection.Message,
                    ValidationErrors.Common.StreetNameStatusInvalidForCorrection.Code),
                AddressHasInvalidStatusException => new TicketError(
                    ValidationErrors.CorrectApproval.AddressInvalidStatus.Message,
                    ValidationErrors.CorrectApproval.AddressInvalidStatus.Code),
                AddressIsNotOfficiallyAssignedException => new TicketError(
                    ValidationErrors.CorrectApproval.AddressIsNotOfficiallyAssigned.Message,
                    ValidationErrors.CorrectApproval.AddressIsNotOfficiallyAssigned.Code),
                _ => null
            };
        }
    }
}
