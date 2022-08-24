namespace AddressRegistry.Api.Legacy.Address.Query
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Be.Vlaanderen.Basisregisters.GrAr.Common;
    using Be.Vlaanderen.Basisregisters.GrAr.Legacy;
    using Be.Vlaanderen.Basisregisters.GrAr.Legacy.Adres;
    using Be.Vlaanderen.Basisregisters.GrAr.Legacy.Bosa;
    using Be.Vlaanderen.Basisregisters.Utilities;
    using Convertors;
    using Infrastructure.Options;
    using Microsoft.EntityFrameworkCore;
    using Projections.Legacy.AddressDetailV2;
    using Projections.Syndication.Municipality;
    using Projections.Syndication.StreetName;
    using Requests;
    using Responses;

    public class AddressBosaQueryV2
    {
        private readonly AddressBosaContext _context;
        private readonly ResponseOptions _responseOptions;

        public AddressBosaQueryV2(
            AddressBosaContext context,
            ResponseOptions responseOptions)
        {
            _context = context;
            _responseOptions = responseOptions;
        }

        public async Task<AddressBosaResponse> Filter(BosaAddressRequest filter)
        {
            var addressesQuery = _context.AddressDetailV2.AsNoTracking()
                .OrderBy(x => x.AddressPersistentLocalId)
                .Where(x => !x.Removed);
            var streetNamesQuery = _context.StreetNameBosaItems.AsNoTracking().Where(x => x.IsComplete);
            var municipalitiesQuery = _context.MunicipalityConsumerLatestItems.AsNoTracking();

            if (filter?.IsOnlyAdresIdRequested == true && int.TryParse(filter.AdresCode?.ObjectId, out var adresId))
            {
                addressesQuery = addressesQuery
                    .Where(a => a.AddressPersistentLocalId == adresId)
                    .ToList()
                    .AsQueryable();

                var address = addressesQuery.FirstOrDefault();
                if (address == null)
                    return new AddressBosaResponse { Adressen = new List<AddressBosaResponseItem>() };

                streetNamesQuery = (await streetNamesQuery
                    .Where(x => x.PersistentLocalId == address.StreetNamePersistentLocalId.ToString())
                    .ToListAsync())
                    .AsQueryable();

                var streetName = streetNamesQuery.FirstOrDefault();

                municipalitiesQuery = (await municipalitiesQuery
                    .Where(x => x.NisCode == streetName.NisCode)
                    .ToListAsync())
                    .AsQueryable();
            }

            var gemeenteCodeVersieId = filter?.GemeenteCode?.VersieId == null ? null : new Rfc3339SerializableDateTimeOffset(filter.GemeenteCode.VersieId.Value).ToString();

            var filteredMunicipalities = FilterMunicipalities(
                filter?.GemeenteCode?.ObjectId,
                gemeenteCodeVersieId,
                filter?.Gemeentenaam?.Spelling,
                filter?.Gemeentenaam?.Taal,
                filter?.Gemeentenaam?.SearchType ?? BosaSearchType.Bevat,
                municipalitiesQuery);

            var straatnaamCodeVersieId = filter?.StraatnaamCode?.VersieId == null ? null : new Rfc3339SerializableDateTimeOffset(filter.StraatnaamCode.VersieId.Value).ToString();
            var filteredStreetNames = FilterStreetNames(
                filter?.StraatnaamCode?.ObjectId,
                straatnaamCodeVersieId,
                filter?.Straatnaam?.Spelling,
                filter?.Straatnaam?.Taal,
                filter?.Straatnaam?.SearchType ?? BosaSearchType.Bevat,
                streetNamesQuery,
                filteredMunicipalities);

            var filteredAddresses =
                FilterAddresses(
                    filter?.AdresCode?.ObjectId,
                    filter?.AdresCode?.VersieId,
                    filter?.Huisnummer,
                    filter?.Busnummer,
                    filter?.AdresStatus,
                    filter?.PostCode?.ObjectId,
                    addressesQuery,
                    filteredStreetNames)
                .OrderBy(x => x.AddressPersistentLocalId);

            var municipalities = filteredMunicipalities.Select(x => new { x.NisCode, x.Version }).ToList();
            var streetNames = filteredStreetNames.Select(x => new { x.StreetNameId, x.PersistentLocalId, x.Version, x.NisCode }).ToList();

            var topFilteredAddresses = filteredAddresses
                    .Take(1001)
                    .ToList();

            var postalCodesInAddresses = filteredAddresses.Select(x => x.PostalCode).Distinct().ToList();

            var postalCodes = _context
                .PostalInfoLatestItems
                .AsNoTracking()
                .Where(y => postalCodesInAddresses.Contains(y.PostalCode))
                .ToList();

            var addresses = topFilteredAddresses
                .Select(x =>
                {
                    var streetName = streetNames.First(y => y.PersistentLocalId == x.StreetNamePersistentLocalId.ToString());
                    var municipality = municipalities.First(y => y.NisCode == streetName.NisCode);

                    var postalCode = postalCodes
                        .First(y => y.PostalCode == x.PostalCode);

                    return new AddressBosaResponseItem(
                        _responseOptions.PostInfoNaamruimte,
                        _responseOptions.GemeenteNaamruimte,
                        _responseOptions.StraatNaamNaamruimte,
                        _responseOptions.Naamruimte,
                        x.AddressPersistentLocalId,
                        x.Status.ConvertFromAddressStatus(),
                        x.HouseNumber,
                        x.BoxNumber,
                        x.OfficiallyAssigned,
                        AddressMapper.GetAddressPoint(x.Position),
                        AddressMapper.ConvertFromGeometryMethod(x.PositionMethod),
                        AddressMapper.ConvertFromGeometrySpecification(x.PositionSpecification),
                        x.VersionTimestamp.ToBelgianDateTimeOffset(),
                        streetName.PersistentLocalId,
                        streetName.Version,
                        municipality.NisCode,
                        municipality.Version,
                        x.PostalCode,
                        postalCode.Version);
                })
                .ToList();

            return new AddressBosaResponse
            {
                Adressen = addresses
            };
        }

        private static IQueryable<AddressDetailItemV2> FilterAddresses(
            string persistentLocalId,
            DateTimeOffset? version,
            string houseNumber,
            string boxNumber,
            AdresStatus? status,
            string postalCode,
            IQueryable<AddressDetailItemV2> addresses,
            IQueryable<StreetNameBosaItem> streetNames)
        {
            var filteredAddresses = addresses.Join(streetNames,
                address => address.StreetNamePersistentLocalId.ToString(),
                streetName => streetName.PersistentLocalId,
                (address, street) => address);

            if (!string.IsNullOrEmpty(persistentLocalId))
            {
                if (int.TryParse(persistentLocalId, out var addressId))
                    filteredAddresses = filteredAddresses.Where(x => x.AddressPersistentLocalId == addressId);
                else
                    return Enumerable.Empty<AddressDetailItemV2>().AsQueryable();
            }

            if (!string.IsNullOrEmpty(houseNumber))
                filteredAddresses = filteredAddresses.Where(x => x.HouseNumber.StartsWith(houseNumber));

            if (!string.IsNullOrEmpty(boxNumber))
                filteredAddresses = filteredAddresses.Where(x => x.BoxNumber.StartsWith(boxNumber));

            if (status.HasValue)
            {
                var mappedStatus = AddressMapper.ConvertFromAdresStatusV2(status);
                filteredAddresses = filteredAddresses.Where(x => x.Status == mappedStatus);
            }

            if (!string.IsNullOrEmpty(postalCode))
                filteredAddresses = filteredAddresses.Where(x => x.PostalCode == postalCode);

            if (version.HasValue)
                filteredAddresses = filteredAddresses.Where(x => x.VersionTimestampAsDateTimeOffset == version);

            return filteredAddresses;
        }

        // https://github.com/Informatievlaanderen/streetname-registry/blob/550a2398077140993d6e60029a1b831c193fb0ad/src/StreetNameRegistry.Api.Legacy/StreetName/Query/StreetNameBosaQuery.cs#L38
        private static IQueryable<StreetNameBosaItem> FilterStreetNames(
            string persistentLocalId,
            string version,
            string streetName,
            Taal? language,
            BosaSearchType searchType,
            IQueryable<StreetNameBosaItem> streetNames,
            IQueryable<MunicipalityLatestItem> filteredMunicipalities)
        {
            var filtered = streetNames.Join(
                filteredMunicipalities,
                street => street.NisCode,
                municipality => municipality.NisCode,
                (street, municipality) => street);

            if (!string.IsNullOrEmpty(persistentLocalId))
                filtered = filtered.Where(m => m.PersistentLocalId == persistentLocalId);

            if (!string.IsNullOrEmpty(version))
                filtered = filtered.Where(m => m.Version == version);

            if (!string.IsNullOrEmpty(streetName))
                filtered = CompareStreetNameByCompareType(
                    filtered,
                    streetName,
                    language,
                    searchType == BosaSearchType.Bevat);
            else if (language.HasValue)
                filtered = ApplyStreetNameLanguageFilter(filtered, language.Value);

            return filtered;
        }

        private static IQueryable<StreetNameBosaItem> ApplyStreetNameLanguageFilter(IQueryable<StreetNameBosaItem> query, Taal language)
        {
            switch (language)
            {
                default:
                case Taal.NL:
                    return query.Where(m => m.NameDutchSearch != null);

                case Taal.FR:
                    return query.Where(m => m.NameFrenchSearch != null);

                case Taal.DE:
                    return query.Where(m => m.NameGermanSearch != null);

                case Taal.EN:
                    return query.Where(m => m.NameEnglishSearch != null);
            }
        }

        private static IQueryable<StreetNameBosaItem> CompareStreetNameByCompareType(
            IQueryable<StreetNameBosaItem> query,
            string searchValue,
            Taal? language,
            bool isContainsFilter)
        {
            var containsValue = searchValue.SanitizeForBosaSearch();
            if (!language.HasValue)
            {
                return isContainsFilter
                    ? query.Where(i =>
                        EF.Functions.Like(i.NameDutchSearch, $"%{containsValue}%") ||
                        EF.Functions.Like(i.NameFrenchSearch, $"%{containsValue}%") ||
                        EF.Functions.Like(i.NameEnglishSearch, $"%{containsValue}%") ||
                        EF.Functions.Like(i.NameGermanSearch, $"%{containsValue}%"))
                    : query.Where(i =>
                        i.NameDutch.Equals(searchValue) ||
                        i.NameFrench.Equals(searchValue) ||
                        i.NameGerman.Equals(searchValue) ||
                        i.NameEnglish.Equals(searchValue));
            }

            switch (language.Value)
            {
                default:
                case Taal.NL:
                    return isContainsFilter
                        ? query.Where(i => EF.Functions.Like(i.NameDutchSearch, $"%{containsValue}%"))
                        : query.Where(i => i.NameDutch.Equals(searchValue));

                case Taal.FR:
                    return isContainsFilter
                        ? query.Where(i => EF.Functions.Like(i.NameFrenchSearch, $"%{containsValue}%"))
                        : query.Where(i => i.NameFrench.Equals(searchValue));

                case Taal.DE:
                    return isContainsFilter
                        ? query.Where(i => EF.Functions.Like(i.NameGermanSearch, $"%{containsValue}%"))
                        : query.Where(i => i.NameGerman.Equals(searchValue));

                case Taal.EN:
                    return isContainsFilter
                        ? query.Where(i => EF.Functions.Like(i.NameEnglishSearch, $"%{containsValue}%"))
                        : query.Where(i => i.NameEnglish.Equals(searchValue));
            }
        }

        // https://github.com/Informatievlaanderen/municipality-registry/blob/054e52fffe13bb4a09f80bf36d221d34ab0aacaa/src/MunicipalityRegistry.Api.Legacy/Municipality/Query/MunicipalityBosaQuery.cs#L83
        private static IQueryable<MunicipalityLatestItem> FilterMunicipalities(
            string nisCode,
            string version,
            string municipalityName,
            Taal? language,
            BosaSearchType searchType,
            IQueryable<MunicipalityLatestItem> municipalities)
        {
            var filtered = municipalities.Where(x => x.IsFlemishRegion);

            if (!string.IsNullOrEmpty(nisCode))
                filtered = filtered.Where(m => m.NisCode == nisCode);

            if (!string.IsNullOrEmpty(version))
                filtered = filtered.Where(m => m.Version == version);

            if (string.IsNullOrEmpty(municipalityName))
            {
                if (language.HasValue)
                    filtered = ApplyMunicipalityLanguageFilter(filtered, language.Value);

                return filtered;
            }

            filtered = CompareMunicipalityByCompareType(filtered,
                municipalityName,
                language,
                searchType == BosaSearchType.Bevat);

            return filtered;
        }

        private static IQueryable<MunicipalityLatestItem> ApplyMunicipalityLanguageFilter(
            IQueryable<MunicipalityLatestItem> query,
            Taal language)
        {
            switch (language)
            {
                default:
                case Taal.NL:
                    return query.Where(m => m.NameDutchSearch != null);

                case Taal.FR:
                    return query.Where(m => m.NameFrenchSearch != null);

                case Taal.DE:
                    return query.Where(m => m.NameGermanSearch != null);

                case Taal.EN:
                    return query.Where(m => m.NameEnglishSearch != null);
            }
        }

        private static IQueryable<MunicipalityLatestItem> CompareMunicipalityByCompareType(
            IQueryable<MunicipalityLatestItem> query,
            string searchValue,
            Taal? language,
            bool isContainsFilter)
        {
            var containsValue = searchValue.SanitizeForBosaSearch();
            if (!language.HasValue)
            {
                return isContainsFilter
                    ? query.Where(i =>
                        EF.Functions.Like(i.NameDutchSearch, $"%{containsValue}%") ||
                        EF.Functions.Like(i.NameFrenchSearch, $"%{containsValue}%") ||
                        EF.Functions.Like(i.NameGermanSearch, $"%{containsValue}%") ||
                        EF.Functions.Like(i.NameEnglishSearch, $"%{containsValue}%"))
                    : query.Where(i =>
                        i.NameDutch.Equals(searchValue) ||
                        i.NameFrench.Equals(searchValue) ||
                        i.NameGerman.Equals(searchValue) ||
                        i.NameEnglish.Equals(searchValue));
            }

            switch (language.Value)
            {
                default:
                case Taal.NL:
                    return isContainsFilter
                        ? query.Where(i => EF.Functions.Like(i.NameDutchSearch, $"%{containsValue}%"))
                        : query.Where(i => i.NameDutch.Equals(searchValue));

                case Taal.FR:
                    return isContainsFilter
                        ? query.Where(i => EF.Functions.Like(i.NameFrenchSearch, $"%{containsValue}%"))
                        : query.Where(i => i.NameFrench.Equals(searchValue));

                case Taal.DE:
                    return isContainsFilter
                        ? query.Where(i => EF.Functions.Like(i.NameGermanSearch, $"%{containsValue}%"))
                        : query.Where(i => i.NameGerman.Equals(searchValue));

                case Taal.EN:
                    return isContainsFilter
                        ? query.Where(i => EF.Functions.Like(i.NameEnglishSearch, $"%{containsValue}%"))
                        : query.Where(i => i.NameEnglish.Equals(searchValue));
            }
        }
    }
}
