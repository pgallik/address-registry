namespace AddressRegistry.Api.BackOffice.Handlers
{
    using Abstractions;
    using Abstractions.Requests;
    using Address;
    using Be.Vlaanderen.Basisregisters.CommandHandling;
    using Be.Vlaanderen.Basisregisters.CommandHandling.Idempotency;
    using MediatR;
    using StreetName;
    using StreetName.Commands;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Be.Vlaanderen.Basisregisters.Sqs.Responses;

    public sealed class CorrectAddressApprovalHandler : BusHandler, IRequestHandler<AddressCorrectApprovalRequest, ETagResponse>
    {
        private readonly IStreetNames _streetNames;
        private readonly BackOfficeContext _backOfficeContext;
        private readonly IdempotencyContext _idempotencyContext;

        public CorrectAddressApprovalHandler(
            ICommandHandlerResolver bus,
            IStreetNames streetNames,
            BackOfficeContext backOfficeContext,
            IdempotencyContext idempotencyContext)
            : base(bus)
        {
            _streetNames = streetNames;
            _backOfficeContext = backOfficeContext;
            _idempotencyContext = idempotencyContext;
        }

        public async Task<ETagResponse> Handle(AddressCorrectApprovalRequest request, CancellationToken cancellationToken)
        {
            var addressPersistentLocalId =
                new AddressPersistentLocalId(new PersistentLocalId(request.PersistentLocalId));

            var relation = _backOfficeContext.AddressPersistentIdStreetNamePersistentIds
                .Single(x => x.AddressPersistentLocalId == addressPersistentLocalId);

            var streetNamePersistentLocalId = new StreetNamePersistentLocalId(relation.StreetNamePersistentLocalId);

            var cmd = new CorrectAddressApproval(
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

            return new ETagResponse(string.Empty, etag);
        }
    }
}
