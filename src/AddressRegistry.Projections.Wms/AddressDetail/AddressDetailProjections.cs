namespace AddressRegistry.Projections.Wms.AddressDetail
{
    using Address;
    using Address.Events;
    using Be.Vlaanderen.Basisregisters.GrAr.Legacy;
    using Be.Vlaanderen.Basisregisters.GrAr.Legacy.Adres;
    using Be.Vlaanderen.Basisregisters.ProjectionHandling.Connector;
    using Be.Vlaanderen.Basisregisters.ProjectionHandling.SqlStreamStore;
    using Be.Vlaanderen.Basisregisters.Utilities.HexByteConvertor;
    using NetTopologySuite.Geometries;
    using NetTopologySuite.IO;
    using NodaTime;


    [ConnectedProjectionName("WMS adressen")]
    [ConnectedProjectionDescription("Projectie die de adressen data voor het WMS adressenregister voorziet.")]
    public class AddressDetailProjections : ConnectedProjection<WmsContext>
    {
        public static readonly string AdresStatusInGebruik = AdresStatus.InGebruik.ToString();
        public static readonly string AdresStatusGehistoreerd = AdresStatus.Gehistoreerd.ToString();
        public static readonly string AdresStatusVoorgesteld = AdresStatus.Voorgesteld.ToString();
        public static readonly string AdresStatusAfgekeurd = AdresStatus.Afgekeurd.ToString();

        private readonly WKBReader _wkbReader;

        public AddressDetailProjections(WKBReader wkbReader)
        {
            _wkbReader = wkbReader;

            When<Envelope<AddressWasRegistered>>(async (context, message, ct) =>
            {
                await context
                    .AddressDetail
                    .AddAsync(
                        new AddressDetailItem
                        {
                            AddressId = message.Message.AddressId,
                            StreetNameId = message.Message.StreetNameId,
                            HouseNumber = message.Message.HouseNumber
                        },
                        ct);
            });

            When<Envelope<AddressBecameComplete>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    address =>
                    {
                        address.Complete = true;
                        UpdateVersionTimestamp(address, message.Message.Provenance.Timestamp);
                    },
                    ct,
                    updateHouseNumberLabelsAfterAddressUpdate: true);
            });

            When<Envelope<AddressBecameCurrent>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    address =>
                    {
                        address.Status = AdresStatusInGebruik;
                        UpdateVersionTimestamp(address, message.Message.Provenance.Timestamp);
                    },
                    ct,
                    updateHouseNumberLabelsBeforeAddressUpdate: true,
                    updateHouseNumberLabelsAfterAddressUpdate: true);
            });

            When<Envelope<AddressBecameIncomplete>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    address =>
                    {
                        address.Complete = false;
                        address.SetHouseNumberLabel(null);
                        UpdateVersionTimestamp(address, message.Message.Provenance.Timestamp);
                    },
                    ct,
                    updateHouseNumberLabelsBeforeAddressUpdate: true,
                    allowUpdateRemovedAddress: true);
            });

            When<Envelope<AddressBecameNotOfficiallyAssigned>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    item =>
                    {
                        item.OfficiallyAssigned = false;
                        UpdateVersionTimestamp(item, message.Message.Provenance.Timestamp);
                    },
                    ct);
            });

            When<Envelope<AddressOfficialAssignmentWasRemoved>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    item =>
                    {
                        item.OfficiallyAssigned = null;
                        UpdateVersionTimestamp(item, message.Message.Provenance.Timestamp);
                    },
                    ct,
                    allowUpdateRemovedAddress: true);
            });

            When<Envelope<AddressPersistentLocalIdWasAssigned>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    item => { item.PersistentLocalId = message.Message.PersistentLocalId; },
                    ct,
                    allowUpdateRemovedAddress: true); //for some reason this gets updated when being removed
            });

            When<Envelope<AddressPostalCodeWasChanged>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    item =>
                    {
                        item.PostalCode = message.Message.PostalCode;
                        UpdateVersionTimestamp(item, message.Message.Provenance.Timestamp);
                    },
                    ct);
            });

            When<Envelope<AddressPostalCodeWasCorrected>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    item =>
                    {
                        item.PostalCode = message.Message.PostalCode;
                        UpdateVersionTimestamp(item, message.Message.Provenance.Timestamp);
                    },
                    ct);
            });

            When<Envelope<AddressPostalCodeWasRemoved>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    item =>
                    {
                        item.PostalCode = null;
                        UpdateVersionTimestamp(item, message.Message.Provenance.Timestamp);
                    },
                    ct,
                    allowUpdateRemovedAddress: true);
            });

            When<Envelope<AddressStatusWasCorrectedToRemoved>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    address =>
                    {
                        address.Status = null;
                        address.SetHouseNumberLabel(null);
                        UpdateVersionTimestamp(address, message.Message.Provenance.Timestamp);
                    },
                    ct,
                    updateHouseNumberLabelsBeforeAddressUpdate: true,
                    allowUpdateRemovedAddress: true);
            });

            When<Envelope<AddressStatusWasRemoved>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    address =>
                    {
                        address.Status = null;
                        address.SetHouseNumberLabel(null);
                        UpdateVersionTimestamp(address, message.Message.Provenance.Timestamp);
                    },
                    ct,
                    updateHouseNumberLabelsBeforeAddressUpdate: true,
                    allowUpdateRemovedAddress: true);
            });

            When<Envelope<AddressStreetNameWasChanged>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    item =>
                    {
                        item.StreetNameId = message.Message.StreetNameId;
                        UpdateVersionTimestamp(item, message.Message.Provenance.Timestamp);
                    },
                    ct);
            });

            When<Envelope<AddressStreetNameWasCorrected>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    item =>
                    {
                        item.StreetNameId = message.Message.StreetNameId;
                        UpdateVersionTimestamp(item, message.Message.Provenance.Timestamp);
                    },
                    ct);
            });

            When<Envelope<AddressWasCorrectedToCurrent>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    address =>
                    {
                        address.Status = AdresStatusInGebruik;
                        UpdateVersionTimestamp(address, message.Message.Provenance.Timestamp);
                    },
                    ct,
                    updateHouseNumberLabelsBeforeAddressUpdate: true,
                    updateHouseNumberLabelsAfterAddressUpdate: true);
            });

            When<Envelope<AddressWasCorrectedToNotOfficiallyAssigned>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    item =>
                    {
                        item.OfficiallyAssigned = false;
                        UpdateVersionTimestamp(item, message.Message.Provenance.Timestamp);
                    },
                    ct);
            });

            When<Envelope<AddressWasCorrectedToOfficiallyAssigned>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    item =>
                    {
                        item.OfficiallyAssigned = true;
                        UpdateVersionTimestamp(item, message.Message.Provenance.Timestamp);
                    },
                    ct);
            });

            When<Envelope<AddressWasCorrectedToProposed>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    address =>
                    {
                        address.Status = AdresStatusVoorgesteld;
                        UpdateVersionTimestamp(address, message.Message.Provenance.Timestamp);
                    },
                    ct,
                    updateHouseNumberLabelsBeforeAddressUpdate: true,
                    updateHouseNumberLabelsAfterAddressUpdate: true);
            });

            When<Envelope<AddressWasCorrectedToRetired>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    address =>
                    {
                        address.Status = AdresStatusGehistoreerd;
                        UpdateVersionTimestamp(address, message.Message.Provenance.Timestamp);
                    },
                    ct,
                    updateHouseNumberLabelsBeforeAddressUpdate: true,
                    updateHouseNumberLabelsAfterAddressUpdate: true);
            });

            When<Envelope<AddressWasOfficiallyAssigned>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    item =>
                    {
                        item.OfficiallyAssigned = true;
                        UpdateVersionTimestamp(item, message.Message.Provenance.Timestamp);
                    },
                    ct);
            });

            When<Envelope<AddressWasProposed>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    address =>
                    {
                        address.Status = AdresStatusVoorgesteld;
                        UpdateVersionTimestamp(address, message.Message.Provenance.Timestamp);
                    },
                    ct,
                    updateHouseNumberLabelsBeforeAddressUpdate: true,
                    updateHouseNumberLabelsAfterAddressUpdate: true);
            });

            When<Envelope<AddressWasRetired>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    address =>
                    {
                        address.Status = AdresStatusGehistoreerd;
                        UpdateVersionTimestamp(address, message.Message.Provenance.Timestamp);
                    },
                    ct,
                    updateHouseNumberLabelsBeforeAddressUpdate: true,
                    updateHouseNumberLabelsAfterAddressUpdate: true);
            });

            When<Envelope<AddressWasRemoved>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    address =>
                    {
                        address.Removed = true;
                        address.SetHouseNumberLabel(null);
                        UpdateVersionTimestamp(address, message.Message.Provenance.Timestamp);
                    },
                    ct,
                    updateHouseNumberLabelsBeforeAddressUpdate: true,
                    allowUpdateRemovedAddress: true);
            });

            When<Envelope<AddressWasPositioned>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    address =>
                    {
                        address.SetPosition(ParsePosition(message.Message.ExtendedWkbGeometry));
                        address.PositionMethod =
                            ConvertGeometryMethodToString(message.Message.GeometryMethod)?.ToString();
                        address.PositionSpecification =
                            MapGeometrySpecificationToPositieSpecificatie(message.Message.GeometrySpecification)
                                ?.ToString();
                        UpdateVersionTimestamp(address, message.Message.Provenance.Timestamp);
                    },
                    ct,
                    updateHouseNumberLabelsBeforeAddressUpdate: true,
                    updateHouseNumberLabelsAfterAddressUpdate: true,
                    allowUpdateRemovedAddress: true);
            });

            When<Envelope<AddressHouseNumberWasChanged>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    address =>
                    {
                        address.HouseNumber = message.Message.HouseNumber;
                        UpdateVersionTimestamp(address, message.Message.Provenance.Timestamp);
                    },
                    ct,
                    updateHouseNumberLabelsAfterAddressUpdate: true);
            });

            When<Envelope<AddressHouseNumberWasCorrected>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    address =>
                    {
                        address.HouseNumber = message.Message.HouseNumber;
                        UpdateVersionTimestamp(address, message.Message.Provenance.Timestamp);
                    },
                    ct,
                    updateHouseNumberLabelsAfterAddressUpdate: true);
            });

            When<Envelope<AddressPositionWasCorrected>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    address =>
                    {
                        address.SetPosition(ParsePosition(message.Message.ExtendedWkbGeometry));
                        address.PositionMethod =
                            ConvertGeometryMethodToString(message.Message.GeometryMethod)?.ToString();
                        address.PositionSpecification =
                            MapGeometrySpecificationToPositieSpecificatie(message.Message.GeometrySpecification)
                                ?.ToString();
                        UpdateVersionTimestamp(address, message.Message.Provenance.Timestamp);
                    },
                    ct,
                    updateHouseNumberLabelsBeforeAddressUpdate: true,
                    updateHouseNumberLabelsAfterAddressUpdate: true,
                    allowUpdateRemovedAddress: true);
            });

            When<Envelope<AddressPositionWasRemoved>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    address =>
                    {
                        address.SetPosition(null);
                        address.PositionMethod = null;
                        address.PositionSpecification = null;
                        address.SetHouseNumberLabel(null);
                        UpdateVersionTimestamp(address, message.Message.Provenance.Timestamp);
                    },
                    ct,
                    updateHouseNumberLabelsBeforeAddressUpdate: true,
                    allowUpdateRemovedAddress: true);
            });

            When<Envelope<AddressBoxNumberWasChanged>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    item =>
                    {
                        item.BoxNumber = message.Message.BoxNumber;
                        item.LabelType = string.IsNullOrWhiteSpace(message.Message.BoxNumber)
                            ? WmsAddressLabelType.HouseNumber
                            : WmsAddressLabelType.BusNumber;
                        UpdateVersionTimestamp(item, message.Message.Provenance.Timestamp);
                    },
                    ct);
            });

            When<Envelope<AddressBoxNumberWasCorrected>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    item =>
                    {
                        item.BoxNumber = message.Message.BoxNumber;
                        item.LabelType = string.IsNullOrWhiteSpace(message.Message.BoxNumber)
                            ? WmsAddressLabelType.HouseNumber
                            : WmsAddressLabelType.BusNumber;
                        UpdateVersionTimestamp(item, message.Message.Provenance.Timestamp);
                    },
                    ct);
            });

            When<Envelope<AddressBoxNumberWasRemoved>>(async (context, message, ct) =>
            {
                await context.FindAndUpdateAddressDetail(
                    message.Message.AddressId,
                    item =>
                    {
                        item.BoxNumber = null;
                        item.LabelType = WmsAddressLabelType.HouseNumber;
                        UpdateVersionTimestamp(item, message.Message.Provenance.Timestamp);
                    },
                    ct,
                    allowUpdateRemovedAddress: true);
            });
        }

        private Point ParsePosition(string extendedWkbGeometry)
            => (Point)_wkbReader.Read(extendedWkbGeometry.ToByteArray());

        private static void UpdateVersionTimestamp(AddressDetailItem addressDetailItem, Instant versionTimestamp)
            => addressDetailItem.VersionTimestamp = versionTimestamp;

        private static PositieGeometrieMethode? MapGeometryMethodToPositieGeometrieMethode(
            GeometryMethod? geometryMethod)
        {
            if (geometryMethod == null)
            {
                return null;
            }

            switch (geometryMethod)
            {
                case GeometryMethod.Interpolated:
                    return PositieGeometrieMethode.Geinterpoleerd;
                case GeometryMethod.AppointedByAdministrator:
                    return PositieGeometrieMethode.AangeduidDoorBeheerder;
                case GeometryMethod.DerivedFromObject:
                    return PositieGeometrieMethode.AfgeleidVanObject;
                default:
                    return null;
            }
        }

        private static string? ConvertGeometryMethodToString(GeometryMethod? method) =>
            MapGeometryMethodToPositieGeometrieMethode(method)?
                .ToString()
                .Replace("Geinterpoleerd", "Geïnterpoleerd");


        private static PositieSpecificatie? MapGeometrySpecificationToPositieSpecificatie(
            GeometrySpecification? geometrySpecification)
        {
            if (geometrySpecification == null)
            {
                return null;
            }

            switch (geometrySpecification)
            {
                case GeometrySpecification.Municipality:
                    return PositieSpecificatie.Gemeente;
                case GeometrySpecification.Street:
                    return PositieSpecificatie.Straat;
                case GeometrySpecification.Parcel:
                    return PositieSpecificatie.Perceel;
                case GeometrySpecification.Lot:
                    return PositieSpecificatie.Lot;
                case GeometrySpecification.Stand:
                    return PositieSpecificatie.Standplaats;
                case GeometrySpecification.Berth:
                    return PositieSpecificatie.Ligplaats;
                case GeometrySpecification.Building:
                    return PositieSpecificatie.Gebouw;
                case GeometrySpecification.BuildingUnit:
                    return PositieSpecificatie.Gebouweenheid;
                case GeometrySpecification.Entry:
                    return PositieSpecificatie.Ingang;
                case GeometrySpecification.RoadSegment:
                    return PositieSpecificatie.Wegsegment;
                default:
                    return null;
            }
        }
    }
}
