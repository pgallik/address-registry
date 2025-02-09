namespace AddressRegistry.Tests.ProjectionTests.Legacy.Extensions
{
    using AddressRegistry.StreetName;
    using AddressRegistry.StreetName.Events;
    using Be.Vlaanderen.Basisregisters.GrAr.Provenance;

    public static class AddressWasDeregulatedExtensions
    {
        public static AddressWasDeregulated WithStreetNamePersistentLocalId(
            this AddressWasDeregulated @event,
            StreetNamePersistentLocalId streetNamePersistentLocalId)
        {
            var newEvent = new AddressWasDeregulated(
                streetNamePersistentLocalId,
                new AddressPersistentLocalId(@event.AddressPersistentLocalId));
            ((ISetProvenance)newEvent).SetProvenance(@event.Provenance.ToProvenance());

            return newEvent;
        }

        public static AddressWasDeregulated WithAddressPersistentLocalId(
            this AddressWasDeregulated @event,
            AddressPersistentLocalId addressPersistentLocalId)
        {
            var newEvent = new AddressWasDeregulated(
                new StreetNamePersistentLocalId(@event.StreetNamePersistentLocalId),
                addressPersistentLocalId);
            ((ISetProvenance)newEvent).SetProvenance(@event.Provenance.ToProvenance());

            return newEvent;
        }
    }
}
