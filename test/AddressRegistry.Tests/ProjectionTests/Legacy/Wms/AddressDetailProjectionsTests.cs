namespace AddressRegistry.Tests.ProjectionTests.Legacy.Wms
{
    using System.Threading.Tasks;
    using Address;
    using Address.Events;
    using AddressRegistry.Projections.Wms;
    using AddressRegistry.Projections.Wms.AddressDetail;
    using AutoFixture;
    using Be.Vlaanderen.Basisregisters.GrAr.Legacy.Adres;
    using Be.Vlaanderen.Basisregisters.GrAr.Provenance;
    using Be.Vlaanderen.Basisregisters.ProjectionHandling.Connector.Testing;
    using Microsoft.EntityFrameworkCore;
    using NetTopologySuite.IO;
    using Xunit;
    using Xunit.Abstractions;

    public class AddressDetailWmsProjectionsTests : ProjectionTest<WmsContext, AddressDetailProjections>
    {
        private readonly WKBReader _wkbReader = WKBReaderFactory.CreateForLegacy();

        public AddressDetailWmsProjectionsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory, DefaultData]
        public async Task AddressBoxNumberWasChangedChangesBoxNumber(
            AddressWasRegistered addressWasRegistered,
            AddressBoxNumberWasChanged addressBoxNumberWasChanged)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressBoxNumberWasChanged)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        BoxNumber = addressBoxNumberWasChanged.BoxNumber,
                        LabelType = WmsAddressLabelType.BusNumber,
                        VersionTimestamp = addressBoxNumberWasChanged.Provenance.Timestamp
                    }));
        }

        [Theory,DefaultData]
        public async Task AddressBoxNumberWasCorrectedChangesBoxNumber(
            AddressWasRegistered addressWasRegistered,
            AddressBoxNumberWasCorrected addressBoxNumberWasCorrected)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressBoxNumberWasCorrected)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        BoxNumber = addressBoxNumberWasCorrected.BoxNumber,
                        LabelType = WmsAddressLabelType.BusNumber,
                        VersionTimestamp = addressBoxNumberWasCorrected.Provenance.Timestamp
                    }));
        }

        [Theory, DefaultData]
        public async Task AddressBoxNumberWasRemovedClearsBoxNumber(
            AddressWasRegistered addressWasRegistered,
            AddressBoxNumberWasChanged addressBoxNumberWasChanged,
            AddressBoxNumberWasRemoved addressBoxNumberWasRemoved)
        {
            await Assert(
                Given(addressWasRegistered,
                    addressBoxNumberWasChanged,
                        addressBoxNumberWasRemoved)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        BoxNumber = null,
                        LabelType = WmsAddressLabelType.HouseNumber,
                        VersionTimestamp = addressBoxNumberWasRemoved.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressWasRegisteredCreatesAddressDetailItem(
            AddressWasRegistered addressWasRegistered)
        {
            await Assert(
                Given(addressWasRegistered)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressBecameCompleteSetsCompletedTrue(
            AddressWasRegistered addressWasRegistered,
            AddressBecameComplete addressBecameComplete)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressBecameComplete)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        Complete = true,
                        VersionTimestamp = addressBecameComplete.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressBecameCurrentSetsStatusToCurrent(
            AddressWasRegistered addressWasRegistered,
            AddressBecameCurrent addressBecameCurrent)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressBecameCurrent)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        Status = AdresStatus.InGebruik.ToString(),
                        VersionTimestamp = addressBecameCurrent.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressBecameIncompleteSetsCompletedFalse(
            AddressWasRegistered addressWasRegistered,
            AddressBecameComplete addressBecameComplete,
            AddressBecameIncomplete addressBecameIncomplete)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressBecameComplete,
                        addressBecameIncomplete)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        Complete = false,
                        VersionTimestamp = addressBecameIncomplete.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressBecameNotOfficallyAssignedSetsOfficallyAssignedFalse(
            AddressWasRegistered addressWasRegistered,
            AddressWasOfficiallyAssigned addressWasOfficiallyAssigned,
            AddressBecameNotOfficiallyAssigned addressBecameNotOfficiallyAssigned)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressWasOfficiallyAssigned,
                        addressBecameNotOfficiallyAssigned)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        OfficiallyAssigned = false,
                        VersionTimestamp = addressBecameNotOfficiallyAssigned.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressHouseNumberWasChangedSetsHouseNumber(
            AddressId addressId,
            Provenance provenance,
            AddressWasRegistered addressWasRegistered)
        {
            var addressHouseNumberWasChanged = new AddressHouseNumberWasChanged(addressId, new HouseNumber("17"));
            ((ISetProvenance)addressHouseNumberWasChanged).SetProvenance(provenance);
            await Assert(
                Given(addressWasRegistered,
                        addressHouseNumberWasChanged)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressHouseNumberWasChanged.HouseNumber,
                        VersionTimestamp = addressHouseNumberWasChanged.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressHouseNumberWasCorrectedSetsHouseNumber(
            AddressId addressId,
            Provenance provenance,
            AddressWasRegistered addressWasRegistered)
        {
            var addressHouseNumberWasCorrected = new AddressHouseNumberWasCorrected(addressId, new HouseNumber("17"));
            ((ISetProvenance)addressHouseNumberWasCorrected).SetProvenance(provenance);
            await Assert(
                Given(addressWasRegistered,
                        addressHouseNumberWasCorrected)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressHouseNumberWasCorrected.HouseNumber,
                        VersionTimestamp = addressHouseNumberWasCorrected.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressOfficialAssignmentWasRemovedSetsOfficalAssignmentToNull(
            AddressWasRegistered addressWasRegistered,
            AddressWasOfficiallyAssigned addressWasOfficiallyAssigned,
            AddressOfficialAssignmentWasRemoved addressOfficialAssignmentWasRemoved)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressWasOfficiallyAssigned,
                        addressOfficialAssignmentWasRemoved)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        OfficiallyAssigned = null,
                        VersionTimestamp = addressOfficialAssignmentWasRemoved.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressPersistentLocalIdWasAssignedSetsPersistentLocalId(
            AddressWasRegistered addressWasRegistered,
            AddressPersistentLocalIdWasAssigned addressPersistentLocalIdWasAssigned)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressPersistentLocalIdWasAssigned)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        PersistentLocalId = addressPersistentLocalIdWasAssigned.PersistentLocalId
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressPositionWasRemovedSetsPositionToNull(
            AddressId addressId,
            WkbGeometry geometry,
            Provenance provenance,
            AddressWasRegistered addressWasRegistered,
            AddressPositionWasRemoved addressPositionWasRemoved)
        {
            var addressPositionWasCorrected =
                new AddressPositionWasCorrected(addressId, new AddressGeometry(GeometryMethod.AppointedByAdministrator, GeometrySpecification.Entry, GeometryHelpers.CreateEwkbFrom(geometry)));

            ((ISetProvenance)addressPositionWasCorrected).SetProvenance(provenance);

            var expected = new AddressDetailItem
            {
                AddressId = addressWasRegistered.AddressId,
                StreetNameId = addressWasRegistered.StreetNameId,
                HouseNumber = addressWasRegistered.HouseNumber,
                PositionSpecification = null,
                PositionMethod = null,
                VersionTimestamp = addressPositionWasRemoved.Provenance.Timestamp
            };
            expected.SetPosition(null);

            await Assert(
                Given(addressWasRegistered,
                        addressPositionWasCorrected, addressPositionWasRemoved)
                    .Expect(ctx => ctx.AddressDetail, expected));
        }

        [Theory]
        [DefaultData]
        public async Task AddressPostalCodeWasChangedSetsPostalCode(
                    AddressWasRegistered addressWasRegistered,
                    AddressPostalCodeWasChanged addressPostalCodeWasChanged)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressPostalCodeWasChanged)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        PostalCode = addressPostalCodeWasChanged.PostalCode,
                        VersionTimestamp = addressPostalCodeWasChanged.Provenance.Timestamp
                    }));
        }
        [Theory]
        [DefaultData]
        public async Task AddressPostalCodeWasCorrectedSetsPostalCode(
                    AddressWasRegistered addressWasRegistered,
                    AddressPostalCodeWasCorrected addressPostalCodeWasCorrected)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressPostalCodeWasCorrected)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        PostalCode = addressPostalCodeWasCorrected.PostalCode,
                        VersionTimestamp = addressPostalCodeWasCorrected.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressPostalCodeWasRemovedSetsPostalCodeToNull(
            AddressWasRegistered addressWasRegistered,
            AddressPostalCodeWasChanged addressPostalCodeWasChanged,
            AddressPostalCodeWasRemoved addressPostalCodeWasRemoved)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressPostalCodeWasChanged,
                        addressPostalCodeWasRemoved)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        PostalCode = null,
                        VersionTimestamp = addressPostalCodeWasRemoved.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressStatusWasCorrectedToRemovedSetsStatusToNull(
            AddressWasRegistered addressWasRegistered,
            AddressBecameCurrent addressBecameCurrent,
            AddressStatusWasCorrectedToRemoved addressStatusWasCorrectedToRemoved)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressBecameCurrent,
                        addressStatusWasCorrectedToRemoved)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        Status = null,
                        VersionTimestamp = addressStatusWasCorrectedToRemoved.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressStatusWasRemovedSetsStatusToNull(
            AddressWasRegistered addressWasRegistered,
            AddressBecameCurrent addressBecameCurrent,
            AddressStatusWasRemoved addressStatusWasRemoved)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressBecameCurrent,
                        addressStatusWasRemoved)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        Status = null,
                        VersionTimestamp = addressStatusWasRemoved.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressWasCorrectedToCurrentSetsStatusToCurrent(
            AddressWasRegistered addressWasRegistered,
            AddressWasCorrectedToCurrent addressWasCorrectedToCurrent)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressWasCorrectedToCurrent)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        Status = AdresStatus.InGebruik.ToString(),
                        VersionTimestamp = addressWasCorrectedToCurrent.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressWasCorrectedToNotOfficiallyAssignedSetsOfficallyAssignedFalse(
            AddressWasRegistered addressWasRegistered,
            AddressWasCorrectedToNotOfficiallyAssigned addressWasCorrectedToNotOfficiallyAssigned)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressWasCorrectedToNotOfficiallyAssigned)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        OfficiallyAssigned = false,
                        VersionTimestamp = addressWasCorrectedToNotOfficiallyAssigned.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressWasCorrectedToOfficiallyAssignedSetsOfficallyAssignedTrue(
            AddressWasRegistered addressWasRegistered,
            AddressWasCorrectedToOfficiallyAssigned addressWasCorrectedToOfficiallyAssigned)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressWasCorrectedToOfficiallyAssigned)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        OfficiallyAssigned = true,
                        VersionTimestamp = addressWasCorrectedToOfficiallyAssigned.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressWasCorrectedToProposedSetsStatusToProposed(
            AddressWasRegistered addressWasRegistered,
            AddressWasCorrectedToProposed addressWasCorrectedToProposed)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressWasCorrectedToProposed)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        Status = AdresStatus.Voorgesteld.ToString(),
                        VersionTimestamp = addressWasCorrectedToProposed.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressWasCorrectedToRetiredSetsStatusToRetired(
            AddressWasRegistered addressWasRegistered,
            AddressWasCorrectedToRetired addressWasCorrectedToRetired)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressWasCorrectedToRetired)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        Status = AdresStatus.Gehistoreerd.ToString(),
                        VersionTimestamp = addressWasCorrectedToRetired.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressWasOfficiallyAssignedSetsOfficallyAssignedTrue(
            AddressWasRegistered addressWasRegistered,
            AddressWasOfficiallyAssigned addressWasOfficiallyAssigned)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressWasOfficiallyAssigned)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        OfficiallyAssigned = true,
                        VersionTimestamp = addressWasOfficiallyAssigned.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressWasProposedSetsStatusToProposed(
            AddressWasRegistered addressWasRegistered,
            AddressWasProposed addressWasProposed)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressWasProposed)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        Status = AdresStatus.Voorgesteld.ToString(),
                        VersionTimestamp = addressWasProposed.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressWasRemovedDeletesRecord(
            AddressWasRegistered addressWasRegistered,
            AddressWasRemoved addressWasRemoved)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressWasRemoved)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        Removed = true,
                        VersionTimestamp = addressWasRemoved.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressWasRetiredSetsStatusToRetired(
            AddressWasRegistered addressWasRegistered,
            AddressWasRetired addressWasRetired)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressWasRetired)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        Status = AdresStatus.Gehistoreerd.ToString(),
                        VersionTimestamp = addressWasRetired.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressPositionWasRemovedAfterRemoveIsSetToNull(
            AddressWasRegistered addressWasRegistered,
            AddressWasRemoved addressWasRemoved,
            AddressPositionWasRemoved addressPositionWasRemoved)
        {
            var expected = new AddressDetailItem
            {
                AddressId = addressWasRegistered.AddressId,
                StreetNameId = addressWasRegistered.StreetNameId,
                HouseNumber = addressWasRegistered.HouseNumber,
                PositionMethod = null,
                PositionSpecification = null,
                Removed = true,
                VersionTimestamp = addressPositionWasRemoved.Provenance.Timestamp
            };
            expected.SetPosition(null);

            await Assert(
                Given(addressWasRegistered,
                        addressWasRemoved,
                        addressPositionWasRemoved)
                    .Expect(ctx => ctx.AddressDetail, expected));
        }

        [Theory]
        [DefaultData]
        public async Task AddressPersistentLocalIdWasAssignedAfterRemoveIsSet(
            AddressWasRegistered addressWasRegistered,
            AddressWasRemoved addressWasRemoved,
            AddressPersistentLocalIdWasAssigned addressPersistentLocalIdWasAssigned)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressWasRemoved,
                        addressPersistentLocalIdWasAssigned)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        PersistentLocalId = addressPersistentLocalIdWasAssigned.PersistentLocalId,
                        Removed = true,
                        VersionTimestamp = addressWasRemoved.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressPostalCodeWasRemovedAfterRemoveIsSetToNull(
            AddressWasRegistered addressWasRegistered,
            AddressWasRemoved addressWasRemoved,
            AddressPostalCodeWasRemoved addressPostalCodeWasRemoved)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressWasRemoved,
                        addressPostalCodeWasRemoved)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        PostalCode = null,
                        Removed = true,
                        VersionTimestamp = addressPostalCodeWasRemoved.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressStatusWasRemovedAfterRemoveIsSetToNull(
            AddressWasRegistered addressWasRegistered,
            AddressWasRemoved addressWasRemoved,
            AddressStatusWasRemoved addressStatusWasRemoved)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressWasRemoved,
                        addressStatusWasRemoved)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        Status = null,
                        Removed = true,
                        VersionTimestamp = addressStatusWasRemoved.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressOfficialAssignmentWasRemovedAfterRemoveIsSetToNull(
            AddressWasRegistered addressWasRegistered,
            AddressWasRemoved addressWasRemoved,
            AddressOfficialAssignmentWasRemoved addressOfficialAssignmentWasRemoved)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressWasRemoved,
                        addressOfficialAssignmentWasRemoved)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        OfficiallyAssigned = null,
                        Removed = true,
                        VersionTimestamp = addressOfficialAssignmentWasRemoved.Provenance.Timestamp
                    }));
        }

        [Theory]
        [DefaultData]
        public async Task AddressBecameIncompleteAfterRemoveIsSet(
            AddressWasRegistered addressWasRegistered,
            AddressWasRemoved addressWasRemoved,
            AddressBecameIncomplete addressBecameIncomplete)
        {
            await Assert(
                Given(addressWasRegistered,
                        addressWasRemoved,
                        addressBecameIncomplete)
                    .Expect(ctx => ctx.AddressDetail, new AddressDetailItem
                    {
                        AddressId = addressWasRegistered.AddressId,
                        StreetNameId = addressWasRegistered.StreetNameId,
                        HouseNumber = addressWasRegistered.HouseNumber,
                        Complete = false,
                        Removed = true,
                        VersionTimestamp = addressBecameIncomplete.Provenance.Timestamp
                    }));
        }

        protected override WmsContext CreateContext(DbContextOptions<WmsContext> options) => new(options);
        protected override AddressDetailProjections CreateProjection() => new(_wkbReader);
    }
}
