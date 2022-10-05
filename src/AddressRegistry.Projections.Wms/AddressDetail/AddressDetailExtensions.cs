// ReSharper disable CompareOfFloatsByEqualityOperator
namespace AddressRegistry.Projections.Wms.AddressDetail
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Be.Vlaanderen.Basisregisters.ProjectionHandling.Connector;
    using Microsoft.EntityFrameworkCore;
    using StreetName;

    public static class AddressDetailExtensions
    {
        public static void UpdateHouseLabel(this IList<AddressDetailItem> listToUpdate)
        {
            var label = listToUpdate.GetHouseNumberLabel();

            foreach (var item in listToUpdate)
            {
                item.HouseNumberLabel = label;
                item.HouseNumberLabelLength = label?.Length ?? 0;
            }
        }

        public static string? GetHouseNumberLabel(this IList<AddressDetailItem> addresses)
        {
            var orderedAddresses = addresses
                .Where(i => !string.IsNullOrWhiteSpace(i.HouseNumber) && !i.Removed)
                .OrderBy(i => i.HouseNumber, new HouseNumberComparer())
                .ToList();

            if (!orderedAddresses.Any())
            {
                return null;
            }

            var smallestNumber = orderedAddresses.First().HouseNumber;
            var highestNumber = orderedAddresses.Last().HouseNumber;

            return smallestNumber != highestNumber
                ? $"{smallestNumber}-{highestNumber}"
                : smallestNumber;
        }

        public static async Task<IList<AddressDetailItem>> FindSharedPositionAddresses(
            this WmsContext context,
            Guid addressId,
            NetTopologySuite.Geometries.Point? position,
            string? status,
            CancellationToken ct)
        {
            var empty = new List<AddressDetailItem>();

            if (position == null)
                return empty;

            return await context
                .AddressDetail
                .Where(i =>
                    i.PositionX == position.X && i.PositionY == position.Y
                                              && i.AddressId != addressId
                                              && i.Status == status
                                              && i.Removed == false
                                              && i.Complete)
                .ToListAsync(ct);
        }

        public static async Task<AddressDetailItem> FindAddressDetail(
            this WmsContext context,
            Guid addressId,
            CancellationToken ct,
            bool allowRemovedAddress = false)
        {
            // NOTE: We cannot depend on SQL computed columns when facing with bulk insert that needs to perform queries.
            var address = await context.AddressDetail.FindAsync(addressId, cancellationToken: ct);

            if (address == null)
            {
                throw DatabaseItemNotFound(addressId);
            }

            // exclude soft deleted entries, unless allowed
            if (!address.Removed || allowRemovedAddress)
            {
                return address;
            }

            throw DatabaseItemNotFound(addressId);
        }

        public static async Task<AddressDetailItem> FindAndUpdateAddressDetail(
            this WmsContext context,
            Guid addressId,
            Action<AddressDetailItem> updateFunc,
            CancellationToken ct,
            bool allowUpdateRemovedAddress = false)
        {
            var address = await context.FindAddressDetail(addressId, ct, allowUpdateRemovedAddress);
            updateFunc(address);
            return address;
        }

        private static ProjectionItemNotFoundException<AddressDetailProjections> DatabaseItemNotFound(Guid addressId) =>
            new ProjectionItemNotFoundException<AddressDetailProjections>(addressId.ToString("D"));
    }

    public class HouseNumberComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            var xInt = int.Parse(HouseNumber.HouseNumberDigits.Match(x).Value);
            var yInt = int.Parse(HouseNumber.HouseNumberDigits.Match(y).Value);

            if (xInt != yInt) return xInt.CompareTo(yInt);

            var xS = HouseNumber.HouseNumberLetters.Match(x).Value;
            var yS = HouseNumber.HouseNumberLetters.Match(y).Value;
            return xS.CompareTo(yS);
        }
    }
}
