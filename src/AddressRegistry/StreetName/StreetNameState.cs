namespace AddressRegistry.StreetName
{
    using System.Linq;
    using Be.Vlaanderen.Basisregisters.AggregateSource.Snapshotting;
    using Events;

    public partial class StreetName
    {
        public MunicipalityId MunicipalityId { get; private set; }
        public StreetNamePersistentLocalId PersistentLocalId { get; private set; }
        public NisCode? MigratedNisCode { get; private set; }
        public bool IsRemoved { get; private set; }
        public StreetNameStatus Status { get; private set; }

        public StreetNameAddresses StreetNameAddresses { get; } = new StreetNameAddresses();

        internal StreetName(ISnapshotStrategy snapshotStrategy) : this()
        {
            Strategy = snapshotStrategy;
        }

        private StreetName()
        {
            Register<StreetNameSnapshot>(When);

            Register<StreetNameWasImported>(When);
            Register<MigratedStreetNameWasImported>(When);

            Register<StreetNameWasApproved>(When);
            Register<StreetNameWasCorrectedFromApprovedToProposed>(When);
            Register<StreetNameWasRejected>(When);
            Register<StreetNameWasCorrectedFromRejectedToProposed>(When);
            Register<StreetNameWasRetired>(When);
            Register<StreetNameWasCorrectedFromRetiredToCurrent>(When);
            Register<StreetNameWasRemoved>(When);

            Register<AddressWasMigratedToStreetName>(When);
            Register<AddressWasProposedV2>(When);
            Register<AddressWasApproved>(When);
            Register<AddressWasCorrectedFromApprovedToProposed>(When);
            Register<AddressWasCorrectedFromApprovedToProposedBecauseHouseNumberWasCorrected>(When);
            Register<AddressWasRejected>(When);
            Register<AddressWasRejectedBecauseHouseNumberWasRejected>(When);
            Register<AddressWasRejectedBecauseHouseNumberWasRetired>(When);
            Register<AddressWasRejectedBecauseStreetNameWasRetired>(When);
            Register<AddressWasDeregulated>(When);
            Register<AddressWasRegularized>(When);
            Register<AddressWasRetiredV2>(When);
            Register<AddressWasRetiredBecauseHouseNumberWasRetired>(When);
            Register<AddressWasRetiredBecauseStreetNameWasRetired>(When);
            Register<AddressWasCorrectedFromRetiredToCurrent>(When);
            Register<AddressPositionWasChanged>(When);
            Register<AddressPostalCodeWasChangedV2>(When);
            Register<AddressPositionWasCorrectedV2>(When);
            Register<AddressPostalCodeWasCorrectedV2>(When);
            Register<AddressHouseNumberWasCorrectedV2>(When);
            Register<AddressBoxNumberWasCorrectedV2>(When);
            Register<AddressWasRemovedV2>(When);
            Register<AddressWasCorrectedFromRejectedToProposed>(When);
            Register<AddressWasRemovedBecauseHouseNumberWasRemoved>(When);
        }

        private void When(MigratedStreetNameWasImported @event)
        {
            PersistentLocalId = new StreetNamePersistentLocalId(@event.StreetNamePersistentLocalId);
            MigratedNisCode = new NisCode(@event.NisCode);
            Status = @event.StreetNameStatus;
            MunicipalityId = new MunicipalityId(@event.MunicipalityId);
        }

        private void When(StreetNameWasRemoved @event)
        {
            IsRemoved = true;
        }

        private void When(StreetNameWasApproved @event)
        {
            Status = StreetNameStatus.Current;
        }

        private void When(StreetNameWasCorrectedFromApprovedToProposed @event)
        {
            Status = StreetNameStatus.Proposed;
        }

        private void When(StreetNameWasRejected @event)
        {
            Status = StreetNameStatus.Rejected;
        }

        private void When(StreetNameWasCorrectedFromRejectedToProposed @event)
        {
            Status = StreetNameStatus.Proposed;
        }

        private void When(StreetNameWasRetired @event)
        {
            Status = StreetNameStatus.Retired;
        }

        private void When(StreetNameWasCorrectedFromRetiredToCurrent @event)
        {
            Status = StreetNameStatus.Current;
        }

        private void When(StreetNameWasImported @event)
        {
            PersistentLocalId = new StreetNamePersistentLocalId(@event.StreetNamePersistentLocalId);
            Status = @event.StreetNameStatus;
            MunicipalityId = new MunicipalityId(@event.MunicipalityId);
        }

        private void When(AddressWasMigratedToStreetName @event)
        {
            var address = new StreetNameAddress(applier: ApplyChange);
            address.Route(@event);

            if (@event.ParentPersistentLocalId.HasValue)
            {
                var parent = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.ParentPersistentLocalId.Value));
                parent.AddChild(address);
            }

            StreetNameAddresses.Add(address);
        }

        private void When(AddressWasProposedV2 @event)
        {
            var address = new StreetNameAddress(applier: ApplyChange);
            address.Route(@event);

            if (@event.ParentPersistentLocalId.HasValue)
            {
                var parent = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.ParentPersistentLocalId.Value));
                parent.AddChild(address);
            }

            StreetNameAddresses.Add(address);
        }

        private void When(AddressWasApproved @event)
        {
            var addressToApprove = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.AddressPersistentLocalId));
            addressToApprove.Route(@event);
        }

        private void When(AddressWasCorrectedFromApprovedToProposed @event)
        {
            var addressToCorrect = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.AddressPersistentLocalId));
            addressToCorrect.Route(@event);
        }

        private void When(AddressWasCorrectedFromApprovedToProposedBecauseHouseNumberWasCorrected @event)
        {
            var addressToCorrect = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.AddressPersistentLocalId));
            addressToCorrect.Route(@event);
        }

        private void When(AddressWasRejected @event)
        {
            var addressToReject = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.AddressPersistentLocalId));
            addressToReject.Route(@event);
        }

        private void When(AddressWasRejectedBecauseHouseNumberWasRejected @event)
        {
            var addressToReject = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.AddressPersistentLocalId));
            addressToReject.Route(@event);
        }

        private void When(AddressWasRejectedBecauseHouseNumberWasRetired @event)
        {
            var addressToReject = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.AddressPersistentLocalId));
            addressToReject.Route(@event);
        }

        private void When(AddressWasRejectedBecauseStreetNameWasRetired @event)
        {
            var addressToReject = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.AddressPersistentLocalId));
            addressToReject.Route(@event);
        }

        private void When(AddressWasDeregulated @event)
        {
            var addressToDeRegulate = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.AddressPersistentLocalId));
            addressToDeRegulate.Route(@event);
        }

        private void When(AddressWasRegularized @event)
        {
            var addressToRegularize = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.AddressPersistentLocalId));
            addressToRegularize.Route(@event);
        }

        private void When(AddressWasRetiredV2 @event)
        {
            var addressToRetire = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.AddressPersistentLocalId));
            addressToRetire.Route(@event);
        }

        private void When(AddressWasRetiredBecauseHouseNumberWasRetired @event)
        {
            var addressToRetire = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.AddressPersistentLocalId));
            addressToRetire.Route(@event);
        }

        private void When(AddressWasRetiredBecauseStreetNameWasRetired @event)
        {
            var addressToRetire = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.AddressPersistentLocalId));
            addressToRetire.Route(@event);
        }

        private void When(AddressWasCorrectedFromRetiredToCurrent @event)
        {
            var addressToCorrect = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.AddressPersistentLocalId));
            addressToCorrect.Route(@event);
        }

        private void When(AddressPositionWasChanged @event)
        {
            var addressToChange = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.AddressPersistentLocalId));
            addressToChange.Route(@event);
        }

        private void When(AddressPostalCodeWasChangedV2 @event)
        {
            var addressToChange = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.AddressPersistentLocalId));
            addressToChange.Route(@event);
        }

        private void When(AddressPositionWasCorrectedV2 @event)
        {
            var addressToCorrect = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.AddressPersistentLocalId));
            addressToCorrect.Route(@event);
        }

        private void When(AddressPostalCodeWasCorrectedV2 @event)
        {
            var addressToCorrect = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.AddressPersistentLocalId));
            addressToCorrect.Route(@event);
        }

        private void When(AddressHouseNumberWasCorrectedV2 @event)
        {
            var addressToCorrect = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.AddressPersistentLocalId));
            addressToCorrect.Route(@event);
        }

        private void When(AddressBoxNumberWasCorrectedV2 @event)
        {
            var addressToCorrect = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.AddressPersistentLocalId));
            addressToCorrect.Route(@event);
        }

        private void When(AddressWasRemovedV2 @event)
        {
            var addressToRemove = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.AddressPersistentLocalId));
            addressToRemove.Route(@event);
        }

        private void When(AddressWasCorrectedFromRejectedToProposed @event)
        {
            var addressToCorrect = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.AddressPersistentLocalId));
            addressToCorrect.Route(@event);
        }

        private void When(AddressWasRemovedBecauseHouseNumberWasRemoved @event)
        {
            var addressToRemove = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(@event.AddressPersistentLocalId));
            addressToRemove.Route(@event);
        }

        private void When(StreetNameSnapshot @event)
        {
            PersistentLocalId = new StreetNamePersistentLocalId(@event.StreetNamePersistentLocalId);
            MunicipalityId = new MunicipalityId(@event.MunicipalityId);
            MigratedNisCode = string.IsNullOrEmpty(@event.MigratedNisCode) ? null : new NisCode(@event.MigratedNisCode);
            Status = @event.StreetNameStatus;
            IsRemoved = @event.IsRemoved;

            foreach (var address in @event.Addresses.Where(x => !x.ParentId.HasValue))
            {
                var streetNameAddress = new StreetNameAddress(applier: ApplyChange);
                streetNameAddress.RestoreSnapshot(PersistentLocalId, address);

                StreetNameAddresses.Add(streetNameAddress);
            }

            foreach (var address in @event.Addresses.Where(x => x.ParentId.HasValue))
            {
                var parent = StreetNameAddresses.GetByPersistentLocalId(new AddressPersistentLocalId(address.ParentId!.Value));

                var streetNameAddress = new StreetNameAddress(applier: ApplyChange);
                streetNameAddress.RestoreSnapshot(PersistentLocalId, address);
                streetNameAddress.SetParent(parent);

                StreetNameAddresses.Add(streetNameAddress);
                parent.AddChild(streetNameAddress);
            }
        }
    }
}
