namespace AddressRegistry.Tests.ProjectionTests.Legacy.Wms
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AddressRegistry.StreetName;
    using AddressRegistry.StreetName.Events;
    using AutoFixture;
    using Be.Vlaanderen.Basisregisters.GrAr.Common.Pipes;
    using Be.Vlaanderen.Basisregisters.GrAr.Provenance;
    using Be.Vlaanderen.Basisregisters.ProjectionHandling.SqlStreamStore;
    using Be.Vlaanderen.Basisregisters.Utilities.HexByteConvertor;
    using Extensions;
    using FluentAssertions;
    using global::AutoFixture;
    using NetTopologySuite.Geometries;
    using NetTopologySuite.IO;
    using Projections.Wms.AddressWmsItem;
    using Xunit;
    using Envelope = Be.Vlaanderen.Basisregisters.ProjectionHandling.SqlStreamStore.Envelope;

    public class AddressWmsItemProjectionTests : AddressWmsItemProjectionTest
    {
        private readonly Fixture _fixture;
        private readonly WKBReader _wkbReader;

        public AddressWmsItemProjectionTests()
        {
            _fixture = new Fixture();
            _fixture.Customize(new WithFixedAddressPersistentLocalId());
            _fixture.Customize(new WithFixedStreetNamePersistentLocalId());
            _fixture.Customize<AddressStatus>(_ => new WithoutUnknownStreetNameAddressStatus());
            _fixture.Customize(new WithValidHouseNumber());
            _fixture.Customize(new WithExtendedWkbGeometry());
            _fixture.Customize(new InfrastructureCustomization());

            _wkbReader = WKBReaderFactory.Create();
        }

        protected override AddressWmsItemProjections CreateProjection()
            =>  new AddressWmsItemProjections(_wkbReader);

        [Fact]
        public async Task WhenAddressWasMigratedToStreetName()
        {
            var addressWasMigratedToStreetName = _fixture.Create<AddressWasMigratedToStreetName>();

            await Sut
                .Given(new Envelope<AddressWasMigratedToStreetName>(new Envelope(addressWasMigratedToStreetName, new Dictionary<string, object>())))
                .Then(async ct =>
                {
                    var addressWmsItem = await ct.AddressWmsItems.FindAsync(addressWasMigratedToStreetName.AddressPersistentLocalId);
                    addressWmsItem.Should().NotBeNull();
                    addressWmsItem!.StreetNamePersistentLocalId.Should().Be(addressWasMigratedToStreetName.StreetNamePersistentLocalId);
                    addressWmsItem.HouseNumber.Should().Be(addressWasMigratedToStreetName.HouseNumber);
                    addressWmsItem.BoxNumber.Should().Be(addressWasMigratedToStreetName.BoxNumber);
                    addressWmsItem.LabelType.Should().Be(Address.WmsAddressLabelType.BusNumber);
                    addressWmsItem.PostalCode.Should().Be(addressWasMigratedToStreetName.PostalCode);
                    addressWmsItem.Status.Should().Be(AddressWmsItemProjections.MapStatus(addressWasMigratedToStreetName.Status));
                    addressWmsItem.OfficiallyAssigned.Should().Be(addressWasMigratedToStreetName.OfficiallyAssigned);
                    addressWmsItem.Position.Should().BeEquivalentTo((Point)_wkbReader.Read(addressWasMigratedToStreetName.ExtendedWkbGeometry.ToByteArray()));
                    addressWmsItem.PositionMethod.Should().Be(AddressWmsItemProjections.ConvertGeometryMethodToString(addressWasMigratedToStreetName.GeometryMethod));
                    addressWmsItem.PositionSpecification.Should().Be(AddressWmsItemProjections.ConvertGeometrySpecificationToString(addressWasMigratedToStreetName.GeometrySpecification));
                    addressWmsItem.Removed.Should().Be(addressWasMigratedToStreetName.IsRemoved);
                    addressWmsItem.VersionTimestamp.Should().Be(addressWasMigratedToStreetName.Provenance.Timestamp);
                });
        }

        [Fact]
        public async Task WhenAddressWasProposedV2()
        {
            var addressWasProposedV2 = _fixture.Create<AddressWasProposedV2>();

            await Sut
                .Given(new Envelope<AddressWasProposedV2>(new Envelope(addressWasProposedV2, new Dictionary<string, object>())))
                .Then(async ct =>
                {
                    var addressWmsItem = await ct.AddressWmsItems.FindAsync(addressWasProposedV2.AddressPersistentLocalId);
                    addressWmsItem.Should().NotBeNull();
                    addressWmsItem!.StreetNamePersistentLocalId.Should().Be(addressWasProposedV2.StreetNamePersistentLocalId);
                    addressWmsItem.HouseNumber.Should().Be(addressWasProposedV2.HouseNumber);
                    addressWmsItem.BoxNumber.Should().Be(addressWasProposedV2.BoxNumber);
                    addressWmsItem.LabelType.Should().Be(Address.WmsAddressLabelType.BusNumber);
                    addressWmsItem.PostalCode.Should().Be(addressWasProposedV2.PostalCode);
                    addressWmsItem.Status.Should().Be(AddressWmsItemProjections.MapStatus(AddressStatus.Proposed));
                    addressWmsItem.OfficiallyAssigned.Should().BeTrue();
                    addressWmsItem.Position.Should().BeEquivalentTo((Point)_wkbReader.Read(addressWasProposedV2.ExtendedWkbGeometry.ToByteArray()));
                    addressWmsItem.PositionMethod.Should().Be(AddressWmsItemProjections.ConvertGeometryMethodToString(addressWasProposedV2.GeometryMethod));
                    addressWmsItem.PositionSpecification.Should().Be(AddressWmsItemProjections.ConvertGeometrySpecificationToString(addressWasProposedV2.GeometrySpecification));
                    addressWmsItem.Removed.Should().BeFalse();
                    addressWmsItem.VersionTimestamp.Should().Be(addressWasProposedV2.Provenance.Timestamp);
                });
        }

        [Fact]
        public async Task WhenAddressWasApproved()
        {
            var addressWasProposedV2 = _fixture.Create<AddressWasProposedV2>();
            var proposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasProposedV2.GetHash() }
            };

            var addressWasApproved = _fixture.Create<AddressWasApproved>();
            var approvedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasApproved.GetHash() }
            };

            await Sut
                .Given(
                    new Envelope<AddressWasProposedV2>(new Envelope(addressWasProposedV2, proposedMetadata)),
                    new Envelope<AddressWasApproved>(new Envelope(addressWasApproved, approvedMetadata)))
                .Then(async ct =>
                {
                    var addressWmsItem = await ct.AddressWmsItems.FindAsync(addressWasApproved.AddressPersistentLocalId);
                    addressWmsItem.Should().NotBeNull();
                    addressWmsItem!.Status.Should().Be(AddressWmsItemProjections.MapStatus(AddressStatus.Current));
                    addressWmsItem.VersionTimestamp.Should().Be(addressWasApproved.Provenance.Timestamp);
                });
        }

        [Fact]
        public async Task WhenAddressWasCorrectedFromApprovedToProposed()
        {
            var addressWasProposedV2 = _fixture.Create<AddressWasProposedV2>();
            var proposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasProposedV2.GetHash() }
            };

            var addressWasApproved = _fixture.Create<AddressWasApproved>();
            var approvedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasApproved.GetHash() }
            };

            var addressApprovalWasCorrected = _fixture.Create<AddressWasCorrectedFromApprovedToProposed>();
            var correctionMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressApprovalWasCorrected.GetHash() }
            };

            await Sut
                .Given(
                    new Envelope<AddressWasProposedV2>(new Envelope(addressWasProposedV2, proposedMetadata)),
                    new Envelope<AddressWasApproved>(new Envelope(addressWasApproved, approvedMetadata)),
                    new Envelope<AddressWasCorrectedFromApprovedToProposed>(new Envelope(addressApprovalWasCorrected, correctionMetadata)))
                .Then(async ct =>
                {
                    var addressWmsItem = await ct.AddressWmsItems.FindAsync(addressWasApproved.AddressPersistentLocalId);
                    addressWmsItem.Should().NotBeNull();
                    addressWmsItem!.Status.Should().Be(AddressWmsItemProjections.MapStatus(AddressStatus.Proposed));
                    addressWmsItem.VersionTimestamp.Should().Be(addressApprovalWasCorrected.Provenance.Timestamp);
                });
        }

        [Fact]
        public async Task WhenAddressWasCorrectedFromApprovedToProposedBecauseHouseNumberWasCorrected()
        {
            var addressWasProposedV2 = _fixture.Create<AddressWasProposedV2>();
            var proposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasProposedV2.GetHash() }
            };

            var addressWasApproved = _fixture.Create<AddressWasApproved>();
            var approvedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasApproved.GetHash() }
            };

            var addressApprovalWasCorrected = _fixture.Create<AddressWasCorrectedFromApprovedToProposedBecauseHouseNumberWasCorrected>();
            var correctionMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressApprovalWasCorrected.GetHash() }
            };

            await Sut
                .Given(
                    new Envelope<AddressWasProposedV2>(new Envelope(addressWasProposedV2, proposedMetadata)),
                    new Envelope<AddressWasApproved>(new Envelope(addressWasApproved, approvedMetadata)),
                    new Envelope<AddressWasCorrectedFromApprovedToProposedBecauseHouseNumberWasCorrected>(new Envelope(addressApprovalWasCorrected, correctionMetadata)))
                .Then(async ct =>
                {
                    var addressWmsItem = await ct.AddressWmsItems.FindAsync(addressWasApproved.AddressPersistentLocalId);
                    addressWmsItem.Should().NotBeNull();
                    addressWmsItem!.Status.Should().Be(AddressWmsItemProjections.MapStatus(AddressStatus.Proposed));
                    addressWmsItem.VersionTimestamp.Should().Be(addressApprovalWasCorrected.Provenance.Timestamp);
                });
        }

        [Fact]
        public async Task WhenAddressWasRejected()
        {
            var addressWasProposedV2 = _fixture.Create<AddressWasProposedV2>();
            var proposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasProposedV2.GetHash() }
            };

            var addressWasRejected = _fixture.Create<AddressWasRejected>();
            var rejectedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasRejected.GetHash() }
            };

            await Sut
                .Given(
                    new Envelope<AddressWasProposedV2>(new Envelope(addressWasProposedV2, proposedMetadata)),
                    new Envelope<AddressWasRejected>(new Envelope(addressWasRejected, rejectedMetadata)))
                .Then(async ct =>
                {
                    var addressWmsItem = await ct.AddressWmsItems.FindAsync(addressWasRejected.AddressPersistentLocalId);
                    addressWmsItem.Should().NotBeNull();
                    addressWmsItem!.Status.Should().Be(AddressWmsItemProjections.MapStatus(AddressStatus.Rejected));
                    addressWmsItem.VersionTimestamp.Should().Be(addressWasRejected.Provenance.Timestamp);
                });
        }

        [Fact]
        public async Task WhenAddressWasRejectedBecauseHouseNumberWasRejected()
        {
            var addressWasProposedV2 = _fixture.Create<AddressWasProposedV2>();
            var proposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasProposedV2.GetHash() }
            };

            var addressWasRejected = _fixture.Create<AddressWasRejectedBecauseHouseNumberWasRejected>();
            var rejectedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasRejected.GetHash() }
            };

            await Sut
                .Given(
                    new Envelope<AddressWasProposedV2>(new Envelope(addressWasProposedV2, proposedMetadata)),
                    new Envelope<AddressWasRejectedBecauseHouseNumberWasRejected>(new Envelope(addressWasRejected, rejectedMetadata)))
                .Then(async ct =>
                {
                    var addressWmsItem = await ct.AddressWmsItems.FindAsync(addressWasRejected.AddressPersistentLocalId);
                    addressWmsItem.Should().NotBeNull();
                    addressWmsItem!.Status.Should().Be(AddressWmsItemProjections.MapStatus(AddressStatus.Rejected));
                    addressWmsItem.VersionTimestamp.Should().Be(addressWasRejected.Provenance.Timestamp);
                });
        }

        [Fact]
        public async Task WhenAddressWasRejectedBecauseHouseNumberWasRetired()
        {
            var addressWasProposedV2 = _fixture.Create<AddressWasProposedV2>();
            var proposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasProposedV2.GetHash() }
            };

            var addressWasRejectedBecauseHouseNumberWasRetired = _fixture.Create<AddressWasRejectedBecauseHouseNumberWasRetired>();
            var rejectedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasRejectedBecauseHouseNumberWasRetired.GetHash() }
            };

            await Sut
                .Given(
                    new Envelope<AddressWasProposedV2>(new Envelope(addressWasProposedV2, proposedMetadata)),
                    new Envelope<AddressWasRejectedBecauseHouseNumberWasRetired>(new Envelope(addressWasRejectedBecauseHouseNumberWasRetired, rejectedMetadata)))
                .Then(async ct =>
                {
                    var addressWmsItem = await ct.AddressWmsItems.FindAsync(addressWasRejectedBecauseHouseNumberWasRetired.AddressPersistentLocalId);
                    addressWmsItem.Should().NotBeNull();
                    addressWmsItem!.Status.Should().Be(AddressWmsItemProjections.MapStatus(AddressStatus.Rejected));
                    addressWmsItem.VersionTimestamp.Should().Be(addressWasRejectedBecauseHouseNumberWasRetired.Provenance.Timestamp);
                });
        }

        [Fact]
        public async Task WhenAddressWasRejectedBecauseStreetNameWasRetired()
        {
            var addressWasProposedV2 = _fixture.Create<AddressWasProposedV2>();
            var proposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasProposedV2.GetHash() }
            };

            var addressWasRejected = _fixture.Create<AddressWasRejectedBecauseStreetNameWasRetired>();
            var rejectedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasRejected.GetHash() }
            };

            await Sut
                .Given(
                    new Envelope<AddressWasProposedV2>(new Envelope(addressWasProposedV2, proposedMetadata)),
                    new Envelope<AddressWasRejectedBecauseStreetNameWasRetired>(new Envelope(addressWasRejected, rejectedMetadata)))
                .Then(async ct =>
                {
                    var addressWmsItem = await ct.AddressWmsItems.FindAsync(addressWasRejected.AddressPersistentLocalId);
                    addressWmsItem.Should().NotBeNull();
                    addressWmsItem!.Status.Should().Be(AddressWmsItemProjections.MapStatus(AddressStatus.Rejected));
                    addressWmsItem.VersionTimestamp.Should().Be(addressWasRejected.Provenance.Timestamp);
                });
        }

        [Fact]
        public async Task WhenAddressWasCorrectedFromRejectedToProposed()
        {
            var addressWasProposedV2 = _fixture.Create<AddressWasProposedV2>();
            var proposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasProposedV2.GetHash() }
            };

            var addressWasRejected = _fixture.Create<AddressWasRejected>();
            var rejectedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasRejected.GetHash() }
            };

            var addressRejectionWasCorrected = _fixture.Create<AddressWasCorrectedFromRejectedToProposed>();
            var correctedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressRejectionWasCorrected.GetHash() }
            };

            await Sut
                .Given(
                    new Envelope<AddressWasProposedV2>(new Envelope(addressWasProposedV2, proposedMetadata)),
                    new Envelope<AddressWasRejected>(new Envelope(addressWasRejected, rejectedMetadata)),
                    new Envelope<AddressWasCorrectedFromRejectedToProposed>(new Envelope(addressRejectionWasCorrected, correctedMetadata)))
                .Then(async ct =>
                {
                    var addressWmsItem = await ct.AddressWmsItems.FindAsync(addressRejectionWasCorrected.AddressPersistentLocalId);
                    addressWmsItem.Should().NotBeNull();
                    addressWmsItem!.Status.Should().Be(AddressWmsItemProjections.MapStatus(AddressStatus.Proposed));
                    addressWmsItem.VersionTimestamp.Should().Be(addressRejectionWasCorrected.Provenance.Timestamp);
                });
        }

        [Fact]
        public async Task WhenAddressWasDeregulated()
        {
            var addressWasProposedV2 = _fixture.Create<AddressWasProposedV2>();
            var proposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasProposedV2.GetHash() }
            };

            var addressWasDeregulated = _fixture.Create<AddressWasDeregulated>();
            var deregulatedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasDeregulated.GetHash() }
            };

            await Sut
                .Given(
                    new Envelope<AddressWasProposedV2>(new Envelope(addressWasProposedV2, proposedMetadata)),
                    new Envelope<AddressWasDeregulated>(new Envelope(addressWasDeregulated, deregulatedMetadata)))
                .Then(async ct =>
                {
                    var addressWmsItem = await ct.AddressWmsItems.FindAsync(addressWasDeregulated.AddressPersistentLocalId);
                    addressWmsItem.Should().NotBeNull();
                    addressWmsItem!.OfficiallyAssigned.Should().BeFalse();
                    addressWmsItem.Status.Should().Be(AddressWmsItemProjections.MapStatus(AddressStatus.Current));
                    addressWmsItem.VersionTimestamp.Should().Be(addressWasDeregulated.Provenance.Timestamp);
                });
        }

        [Fact]
        public async Task WhenAddressWasRetired()
        {
            var addressWasProposedV2 = _fixture.Create<AddressWasProposedV2>();
            var proposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasProposedV2.GetHash() }
            };

            var addressWasApproved = _fixture.Create<AddressWasApproved>();
            var approvedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasApproved.GetHash() }
            };

            var addressWasRetiredV2 = _fixture.Create<AddressWasRetiredV2>();
            var retiredMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasRetiredV2.GetHash() }
            };

            await Sut
                .Given(
                    new Envelope<AddressWasProposedV2>(new Envelope(addressWasProposedV2, proposedMetadata)),
                    new Envelope<AddressWasApproved>(new Envelope(addressWasApproved, approvedMetadata)),
                    new Envelope<AddressWasRetiredV2>(new Envelope(addressWasRetiredV2, retiredMetadata)))
                .Then(async ct =>
                {
                    var addressWmsItem = await ct.AddressWmsItems.FindAsync(addressWasRetiredV2.AddressPersistentLocalId);
                    addressWmsItem.Should().NotBeNull();
                    addressWmsItem!.Status.Should().Be(AddressWmsItemProjections.MapStatus(AddressStatus.Retired));
                    addressWmsItem.VersionTimestamp.Should().Be(addressWasRetiredV2.Provenance.Timestamp);
                });
        }

        [Fact]
        public async Task WhenAddressWasRetiredBecauseHouseNumberWasRetired()
        {
            var addressWasProposedV2 = _fixture.Create<AddressWasProposedV2>();
            var proposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasProposedV2.GetHash() }
            };

            var addressWasApproved = _fixture.Create<AddressWasApproved>();
            var approvedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasApproved.GetHash() }
            };

            var addressWasRetiredBecauseHouseNumberWasRetired = _fixture.Create<AddressWasRetiredBecauseHouseNumberWasRetired>();
            var retiredMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasRetiredBecauseHouseNumberWasRetired.GetHash() }
            };

            await Sut
                .Given(
                    new Envelope<AddressWasProposedV2>(new Envelope(addressWasProposedV2, proposedMetadata)),
                    new Envelope<AddressWasApproved>(new Envelope(addressWasApproved, approvedMetadata)),
                    new Envelope<AddressWasRetiredBecauseHouseNumberWasRetired>(new Envelope(addressWasRetiredBecauseHouseNumberWasRetired, retiredMetadata)))
                .Then(async ct =>
                {
                    var addressWmsItem = await ct.AddressWmsItems.FindAsync(addressWasRetiredBecauseHouseNumberWasRetired.AddressPersistentLocalId);
                    addressWmsItem.Should().NotBeNull();
                    addressWmsItem!.Status.Should().Be(AddressWmsItemProjections.MapStatus(AddressStatus.Retired));
                    addressWmsItem.VersionTimestamp.Should().Be(addressWasRetiredBecauseHouseNumberWasRetired.Provenance.Timestamp);
                });
        }

        [Fact]
        public async Task WhenAddressWasRetiredBecauseStreetNameWasRetired()
        {
            var addressWasProposedV2 = _fixture.Create<AddressWasProposedV2>();
            var proposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasProposedV2.GetHash() }
            };

            var addressWasApproved = _fixture.Create<AddressWasApproved>();
            var approvedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasApproved.GetHash() }
            };

            var addressWasRetiredBecauseHouseNumberWasRetired = _fixture.Create<AddressWasRetiredBecauseStreetNameWasRetired>();
            var retiredMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasRetiredBecauseHouseNumberWasRetired.GetHash() }
            };

            await Sut
                .Given(
                    new Envelope<AddressWasProposedV2>(new Envelope(addressWasProposedV2, proposedMetadata)),
                    new Envelope<AddressWasApproved>(new Envelope(addressWasApproved, approvedMetadata)),
                    new Envelope<AddressWasRetiredBecauseStreetNameWasRetired>(new Envelope(addressWasRetiredBecauseHouseNumberWasRetired, retiredMetadata)))
                .Then(async ct =>
                {
                    var addressWmsItem = await ct.AddressWmsItems.FindAsync(addressWasRetiredBecauseHouseNumberWasRetired.AddressPersistentLocalId);
                    addressWmsItem.Should().NotBeNull();
                    addressWmsItem!.Status.Should().Be(AddressWmsItemProjections.MapStatus(AddressStatus.Retired));
                    addressWmsItem.VersionTimestamp.Should().Be(addressWasRetiredBecauseHouseNumberWasRetired.Provenance.Timestamp);
                });
        }

        [Fact]
        public async Task WhenAddressWasCorrectedFromRetiredToCurrent()
        {
            var addressWasProposedV2 = _fixture.Create<AddressWasProposedV2>();
            var proposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasProposedV2.GetHash() }
            };

            var addressWasApproved = _fixture.Create<AddressWasApproved>();
            var approvedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasApproved.GetHash() }
            };

            var addressWasRetiredV2 = _fixture.Create<AddressWasRetiredV2>();
            var retiredMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasRetiredV2.GetHash() }
            };

            var addressWasCorrectedFromRetiredToCurrent = _fixture.Create<AddressWasCorrectedFromRetiredToCurrent>();
            var correctedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasCorrectedFromRetiredToCurrent.GetHash() }
            };

            await Sut
                .Given(
                    new Envelope<AddressWasProposedV2>(new Envelope(addressWasProposedV2, proposedMetadata)),
                    new Envelope<AddressWasApproved>(new Envelope(addressWasApproved, approvedMetadata)),
                    new Envelope<AddressWasRetiredV2>(new Envelope(addressWasRetiredV2, retiredMetadata)),
                    new Envelope<AddressWasCorrectedFromRetiredToCurrent>(new Envelope(addressWasCorrectedFromRetiredToCurrent, correctedMetadata)))
                .Then(async ct =>
                {
                    var addressWmsItem = await ct.AddressWmsItems.FindAsync(addressWasRetiredV2.AddressPersistentLocalId);
                    addressWmsItem.Should().NotBeNull();
                    addressWmsItem!.Status.Should().Be(AddressWmsItemProjections.MapStatus(AddressStatus.Current));
                    addressWmsItem.VersionTimestamp.Should().Be(addressWasCorrectedFromRetiredToCurrent.Provenance.Timestamp);
                });
        }

        [Fact]
        public async Task WhenAddressPostalCodeWasChangedV2()
        {
            var addressWasProposedV2 = _fixture.Create<AddressWasProposedV2>()
                .WithAddressPersistentLocalId(new AddressPersistentLocalId(1))
                .WithPostalCode(new PostalCode("9000"));
            var proposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasProposedV2.GetHash() }
            };

            var boxNumberAddressWasProposedV2 = _fixture.Create<AddressWasProposedV2>()
                .WithAddressPersistentLocalId(new AddressPersistentLocalId(2))
                .WithPostalCode(new PostalCode("9000"));
            var boxNumberProposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, boxNumberAddressWasProposedV2.GetHash() }
            };

            var addressPostalCodeWasChangedV2 = _fixture.Create<AddressPostalCodeWasChangedV2>()
                .WithAddressPersistentLocalId(new AddressPersistentLocalId(addressWasProposedV2.AddressPersistentLocalId))
                .WithBoxNumberPersistentLocalIds(new [] { new AddressPersistentLocalId(boxNumberAddressWasProposedV2.AddressPersistentLocalId) })
                .WithPostalCode(new PostalCode("2000"));
            var postalCodeWasChangedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressPostalCodeWasChangedV2.GetHash() }
            };

            await Sut
                .Given(
                    new Envelope<AddressWasProposedV2>(new Envelope(addressWasProposedV2, proposedMetadata)),
                    new Envelope<AddressWasProposedV2>(new Envelope(boxNumberAddressWasProposedV2, boxNumberProposedMetadata)),
                    new Envelope<AddressPostalCodeWasChangedV2>(new Envelope(addressPostalCodeWasChangedV2, postalCodeWasChangedMetadata)))
                .Then(async ct =>
                {
                    var addressWmsItem = await ct.AddressWmsItems.FindAsync(addressPostalCodeWasChangedV2.AddressPersistentLocalId);
                    addressWmsItem.Should().NotBeNull();
                    addressWmsItem!.PostalCode.Should().Be(addressPostalCodeWasChangedV2.PostalCode);
                    addressWmsItem.VersionTimestamp.Should().Be(addressPostalCodeWasChangedV2.Provenance.Timestamp);

                    var boxNumberAddressWmsItem = await ct.AddressWmsItems.FindAsync(boxNumberAddressWasProposedV2.AddressPersistentLocalId);
                    boxNumberAddressWmsItem.Should().NotBeNull();
                    boxNumberAddressWmsItem!.PostalCode.Should().BeEquivalentTo(addressPostalCodeWasChangedV2.PostalCode);
                    boxNumberAddressWmsItem.VersionTimestamp.Should().Be(addressPostalCodeWasChangedV2.Provenance.Timestamp);
                });
        }

        [Fact]
        public async Task WhenAddressPostalCodeWasCorrectedV2()
        {
            var addressWasProposedV2 = _fixture.Create<AddressWasProposedV2>()
                .WithAddressPersistentLocalId(new AddressPersistentLocalId(1))
                .WithPostalCode(new PostalCode("9000"));
            var proposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasProposedV2.GetHash() }
            };

            var boxNumberAddressWasProposedV2 = _fixture.Create<AddressWasProposedV2>()
                .WithAddressPersistentLocalId(new AddressPersistentLocalId(2))
                .WithPostalCode(new PostalCode("9000"));
            var boxNumberProposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, boxNumberAddressWasProposedV2.GetHash() }
            };

            var addressPostalCodeWasCorrectedV2 = _fixture.Create<AddressPostalCodeWasCorrectedV2>()
                .WithAddressPersistentLocalId(new AddressPersistentLocalId(addressWasProposedV2.AddressPersistentLocalId))
                .WithBoxNumberPersistentLocalIds(new [] { new AddressPersistentLocalId(boxNumberAddressWasProposedV2.AddressPersistentLocalId) })
                .WithPostalCode(new PostalCode("2000"));
            var postalCodeWasCorrectedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressPostalCodeWasCorrectedV2.GetHash() }
            };

            await Sut
                .Given(
                    new Envelope<AddressWasProposedV2>(new Envelope(addressWasProposedV2, proposedMetadata)),
                    new Envelope<AddressWasProposedV2>(new Envelope(boxNumberAddressWasProposedV2, boxNumberProposedMetadata)),
                    new Envelope<AddressPostalCodeWasCorrectedV2>(new Envelope(addressPostalCodeWasCorrectedV2, postalCodeWasCorrectedMetadata)))
                .Then(async ct =>
                {
                    var addressWmsItem = await ct.AddressWmsItems.FindAsync(addressPostalCodeWasCorrectedV2.AddressPersistentLocalId);
                    addressWmsItem.Should().NotBeNull();
                    addressWmsItem!.PostalCode.Should().Be(addressPostalCodeWasCorrectedV2.PostalCode);
                    addressWmsItem.VersionTimestamp.Should().Be(addressPostalCodeWasCorrectedV2.Provenance.Timestamp);

                    var boxNumberAddressWmsItem = await ct.AddressWmsItems.FindAsync(boxNumberAddressWasProposedV2.AddressPersistentLocalId);
                    boxNumberAddressWmsItem.Should().NotBeNull();
                    boxNumberAddressWmsItem!.PostalCode.Should().BeEquivalentTo(addressPostalCodeWasCorrectedV2.PostalCode);
                    boxNumberAddressWmsItem.VersionTimestamp.Should().Be(addressPostalCodeWasCorrectedV2.Provenance.Timestamp);
                });
        }

        [Fact]
        public async Task WhenAddressHouseNumberWasCorrectedV2()
        {
            var addressWasProposedV2 = _fixture.Create<AddressWasProposedV2>()
                .WithAddressPersistentLocalId(new AddressPersistentLocalId(1))
                .WithHouseNumber(new HouseNumber("101"));
            var proposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasProposedV2.GetHash() }
            };

            var boxNumberAddressWasProposedV2 = _fixture.Create<AddressWasProposedV2>()
                .WithAddressPersistentLocalId(new AddressPersistentLocalId(2))
                .WithHouseNumber(new HouseNumber("101"));
            var boxNumberProposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, boxNumberAddressWasProposedV2.GetHash() }
            };

            var addressHouseNumberWasCorrectedV2 = _fixture.Create<AddressHouseNumberWasCorrectedV2>()
                .WithAddressPersistentLocalId(new AddressPersistentLocalId(addressWasProposedV2.AddressPersistentLocalId))
                .WithBoxNumberPersistentLocalIds(new [] { new AddressPersistentLocalId(boxNumberAddressWasProposedV2.AddressPersistentLocalId) })
                .WithHouseNumber(new HouseNumber("102"));
            var houseNumberWasCorrectedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressHouseNumberWasCorrectedV2.GetHash() }
            };

            await Sut
                .Given(
                    new Envelope<AddressWasProposedV2>(new Envelope(addressWasProposedV2, proposedMetadata)),
                    new Envelope<AddressWasProposedV2>(new Envelope(boxNumberAddressWasProposedV2, boxNumberProposedMetadata)),
                    new Envelope<AddressHouseNumberWasCorrectedV2>(new Envelope(addressHouseNumberWasCorrectedV2, houseNumberWasCorrectedMetadata)))
                .Then(async ct =>
                {
                    var addressWmsItem = await ct.AddressWmsItems.FindAsync(addressHouseNumberWasCorrectedV2.AddressPersistentLocalId);
                    addressWmsItem.Should().NotBeNull();
                    addressWmsItem!.HouseNumber.Should().Be(addressHouseNumberWasCorrectedV2.HouseNumber);
                    addressWmsItem.VersionTimestamp.Should().Be(addressHouseNumberWasCorrectedV2.Provenance.Timestamp);

                    var boxNumberAddressWmsItem = await ct.AddressWmsItems.FindAsync(boxNumberAddressWasProposedV2.AddressPersistentLocalId);
                    boxNumberAddressWmsItem.Should().NotBeNull();
                    boxNumberAddressWmsItem!.HouseNumber.Should().BeEquivalentTo(addressHouseNumberWasCorrectedV2.HouseNumber);
                    boxNumberAddressWmsItem.VersionTimestamp.Should().Be(addressHouseNumberWasCorrectedV2.Provenance.Timestamp);
                });
        }

        [Fact]
        public async Task WhenAddressBoxNumberWasCorrectedV2()
        {
            var addressWasProposedV2 = _fixture.Create<AddressWasProposedV2>()
                .WithAddressPersistentLocalId(new AddressPersistentLocalId(1))
                .WithHouseNumber(new HouseNumber("101"))
                .WithBoxNumber(new BoxNumber("A1"));
            var proposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasProposedV2.GetHash() }
            };

            var addressBoxNumberWasCorrectedV2 = new AddressBoxNumberWasCorrectedV2(
                new StreetNamePersistentLocalId(2),
                new AddressPersistentLocalId(addressWasProposedV2.AddressPersistentLocalId),
                new BoxNumber("1B"));
            ((ISetProvenance)addressBoxNumberWasCorrectedV2).SetProvenance(_fixture.Create<Provenance>());

            var boxNumberWasCorrectedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressBoxNumberWasCorrectedV2.GetHash() }
            };

            await Sut
                .Given(
                    new Envelope<AddressWasProposedV2>(new Envelope(addressWasProposedV2, proposedMetadata)),
                    new Envelope<AddressBoxNumberWasCorrectedV2>(new Envelope(addressBoxNumberWasCorrectedV2, boxNumberWasCorrectedMetadata)))
                .Then(async ct =>
                {
                    var addressWmsItem = await ct.AddressWmsItems.FindAsync(addressBoxNumberWasCorrectedV2.AddressPersistentLocalId);
                    addressWmsItem.Should().NotBeNull();
                    addressWmsItem!.BoxNumber.Should().Be(addressBoxNumberWasCorrectedV2.BoxNumber);
                    addressWmsItem.VersionTimestamp.Should().Be(addressBoxNumberWasCorrectedV2.Provenance.Timestamp);

                    var boxNumberAddressWmsItem = await ct.AddressWmsItems.FindAsync(addressBoxNumberWasCorrectedV2.AddressPersistentLocalId);
                    boxNumberAddressWmsItem.Should().NotBeNull();
                    boxNumberAddressWmsItem!.BoxNumber.Should().BeEquivalentTo(addressBoxNumberWasCorrectedV2.BoxNumber);
                    boxNumberAddressWmsItem.VersionTimestamp.Should().Be(addressBoxNumberWasCorrectedV2.Provenance.Timestamp);
                });
        }

        [Fact]
        public async Task WhenAddressPositionWasChanged()
        {
            var addressWasProposedV2 = _fixture.Create<AddressWasProposedV2>();
            var proposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasProposedV2.GetHash() }
            };

            var addressPositionWasChanged = _fixture.Create<AddressPositionWasChanged>();
            var positionChangedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressPositionWasChanged.GetHash() }
            };

            await Sut
                .Given(
                    new Envelope<AddressWasProposedV2>(new Envelope(addressWasProposedV2, proposedMetadata)),
                    new Envelope<AddressPositionWasChanged>(new Envelope(addressPositionWasChanged, positionChangedMetadata)))
                .Then(async ct =>
                {
                    var addressWmsItem = await ct.AddressWmsItems.FindAsync(addressPositionWasChanged.AddressPersistentLocalId);
                    addressWmsItem.Should().NotBeNull();
                    addressWmsItem!.PositionMethod.Should().Be(AddressWmsItemProjections.ConvertGeometryMethodToString(addressPositionWasChanged.GeometryMethod));
                    addressWmsItem.PositionSpecification.Should().Be(AddressWmsItemProjections.ConvertGeometrySpecificationToString(addressPositionWasChanged.GeometrySpecification));
                    addressWmsItem.Position.Should().Be((Point) _wkbReader.Read(addressPositionWasChanged.ExtendedWkbGeometry.ToByteArray()));
                    addressWmsItem.VersionTimestamp.Should().Be(addressPositionWasChanged.Provenance.Timestamp);
                });
        }

        [Fact]
        public async Task WhenAddressPositionWasCorrectedV2()
        {
            var addressWasProposedV2 = _fixture.Create<AddressWasProposedV2>();
            var proposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasProposedV2.GetHash() }
            };

            var addressPositionWasCorrectedV2 = _fixture.Create<AddressPositionWasCorrectedV2>();
            var positionCorrectedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressPositionWasCorrectedV2.GetHash() }
            };

            await Sut
                .Given(
                    new Envelope<AddressWasProposedV2>(new Envelope(addressWasProposedV2, proposedMetadata)),
                    new Envelope<AddressPositionWasCorrectedV2>(new Envelope(addressPositionWasCorrectedV2, positionCorrectedMetadata)))
                .Then(async ct =>
                {
                    var addressWmsItem = await ct.AddressWmsItems.FindAsync(addressPositionWasCorrectedV2.AddressPersistentLocalId);
                    addressWmsItem.Should().NotBeNull();
                    addressWmsItem!.PositionMethod.Should().Be(AddressWmsItemProjections.ConvertGeometryMethodToString(addressPositionWasCorrectedV2.GeometryMethod));
                    addressWmsItem.PositionSpecification.Should().Be(AddressWmsItemProjections.ConvertGeometrySpecificationToString(addressPositionWasCorrectedV2.GeometrySpecification));
                    addressWmsItem.Position.Should().Be((Point)_wkbReader.Read(addressPositionWasCorrectedV2.ExtendedWkbGeometry.ToByteArray()));
                    addressWmsItem.VersionTimestamp.Should().Be(addressPositionWasCorrectedV2.Provenance.Timestamp);
                });
        }

        [Fact]
        public async Task WhenAddressWasRemovedV2()
        {
            var addressWasProposedV2 = _fixture.Create<AddressWasProposedV2>();
            var proposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasProposedV2.GetHash() }
            };

            var addressWasRemoved = _fixture.Create<AddressWasRemovedV2>();
            var addressWasRemovedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasRemoved.GetHash() }
            };

            await Sut
                .Given(
                    new Envelope<AddressWasProposedV2>(new Envelope(addressWasProposedV2, proposedMetadata)),
                    new Envelope<AddressWasRemovedV2>(new Envelope(addressWasRemoved, addressWasRemovedMetadata)))
                .Then(async ct =>
                {
                    var addressWmsItem = await ct.AddressWmsItems.FindAsync(addressWasRemoved.AddressPersistentLocalId);
                    addressWmsItem.Should().NotBeNull();
                    addressWmsItem!.Removed.Should().BeTrue();
                    addressWmsItem.VersionTimestamp.Should().Be(addressWasRemoved.Provenance.Timestamp);
                });
        }

        [Fact]
        public async Task WhenAddressWasRemovedBecauseHouseNumberWasRemoved()
        {
            var addressWasProposedV2 = _fixture.Create<AddressWasProposedV2>();
            var proposedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasProposedV2.GetHash() }
            };

            var addressWasRemoved = _fixture.Create<AddressWasRemovedBecauseHouseNumberWasRemoved>();
            var addressWasRemovedMetadata = new Dictionary<string, object>
            {
                { AddEventHashPipe.HashMetadataKey, addressWasRemoved.GetHash() }
            };

            await Sut
                .Given(
                    new Envelope<AddressWasProposedV2>(new Envelope(addressWasProposedV2, proposedMetadata)),
                    new Envelope<AddressWasRemovedBecauseHouseNumberWasRemoved>(new Envelope(addressWasRemoved, addressWasRemovedMetadata)))
                .Then(async ct =>
                {
                    var addressWmsItem = await ct.AddressWmsItems.FindAsync(addressWasRemoved.AddressPersistentLocalId);
                    addressWmsItem.Should().NotBeNull();
                    addressWmsItem!.Removed.Should().BeTrue();
                    addressWmsItem.VersionTimestamp.Should().Be(addressWasRemoved.Provenance.Timestamp);
                });
        }
    }
}
