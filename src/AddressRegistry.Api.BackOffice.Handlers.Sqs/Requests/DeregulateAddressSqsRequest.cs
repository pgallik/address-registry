namespace AddressRegistry.Api.BackOffice.Handlers.Sqs.Requests
{
    using Abstractions.Requests;
    using Be.Vlaanderen.Basisregisters.Sqs.Requests;

    public sealed class DeregulateAddressSqsRequest : SqsRequest, IHasBackOfficeRequest<DeregulateAddressBackOfficeRequest>
    {
        public DeregulateAddressBackOfficeRequest Request { get; init; }
    }
}
