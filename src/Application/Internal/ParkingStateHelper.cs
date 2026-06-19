using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.Internal;

internal static class ParkingStateHelper
{
    public static User? GetActiveUser(ParkingState state, Guid userId) =>
        state.Users.FirstOrDefault(user => user.Id == userId && user.IsActive);

    public static bool IsAdmin(ParkingState state, Guid userId)
    {
        var user = GetActiveUser(state, userId);
        return user?.Role == UserRole.Admin;
    }

    public static bool IsOperator(ParkingState state, Guid userId)
    {
        var user = GetActiveUser(state, userId);
        return user?.Role == UserRole.Operator;
    }

    public static bool IsEndUser(ParkingState state, Guid userId)
    {
        var user = GetActiveUser(state, userId);
        return user?.Role == UserRole.User;
    }

    public static ParkingZone? GetZoneById(ParkingState state, Guid zoneId) =>
        state.Zones.FirstOrDefault(zone => zone.Id == zoneId && zone.IsActive);

    public static ParkingSlot? GetSlotByCode(ParkingState state, string slotCode, Guid? zoneId = null)
    {
        if (!InputNormalizer.TryNormalizeRequired(slotCode, out var normalizedCode))
        {
            return null;
        }

        normalizedCode = normalizedCode.ToUpperInvariant();
        return state.Slots.FirstOrDefault(slot =>
            string.Equals(slot.Code, normalizedCode, StringComparison.OrdinalIgnoreCase) &&
            (!zoneId.HasValue || slot.ZoneId == zoneId.Value));
    }

    public static void RecalculateSlotStatus(ParkingState state, Guid slotId)
    {
        var slot = state.Slots.FirstOrDefault(candidate => candidate.Id == slotId);
        if (slot is null || slot.Status == SlotStatus.OutOfService)
        {
            return;
        }

        var hasOpenSession = state.Sessions.Any(session => session.SlotId == slotId && !session.IsClosed);
        if (hasOpenSession)
        {
            slot.Status = SlotStatus.Occupied;
            return;
        }

        var hasActiveReservation = state.Reservations.Any(reservation =>
            reservation.SlotId == slotId && reservation.Status == ReservationStatus.Active);
        slot.Status = hasActiveReservation ? SlotStatus.Reserved : SlotStatus.Available;
    }

    public static void SeedZonesAndSlotsIfNeeded(ParkingState state, IClock clock)
    {
        if (state.Zones.Count > 0)
        {
            return;
        }

        state.Slots.Clear();

        var templates = new[]
        {
            ("CHZ", "Chilonzor markaz", "Chilonzor", "Chilonzor 9-mavze", 41.2856, 69.2034, 6),
            ("YUN", "Yunusobod plaza", "Yunusobod", "Amir Temur shoh ko'chasi 95", 41.3673, 69.2878, 8),
            ("MIR", "Mirzo Ulug'bek", "Mirzo Ulug'bek", "Milliy bog' yonida", 41.3381, 69.3344, 5),
            ("YAK", "Yakkasaroy", "Yakkasaroy", "Shota Rustaveli ko'chasi", 41.2994, 69.2728, 6),
            ("SER", "Sergeli savdo", "Sergeli", "Sergeli-7 mavze", 41.2200, 69.2200, 5),
            ("UCH", "Olmazor", "Olmazor", "Universitet metro yonida", 41.3112, 69.2796, 7)
        };

        foreach (var (code, name, district, address, lat, lng, slotCount) in templates)
        {
            var zone = new ParkingZone
            {
                Code = code,
                Name = name,
                District = district,
                Address = address,
                Latitude = lat,
                Longitude = lng,
                CreatedAtUtc = clock.UtcNow
            };

            state.Zones.Add(zone);

            for (var number = 1; number <= slotCount; number++)
            {
                state.Slots.Add(new ParkingSlot
                {
                    ZoneId = zone.Id,
                    Code = $"{code}-{number:D2}",
                    Status = SlotStatus.Available,
                    HourlyRate = state.DefaultHourlyRate,
                    CreatedAtUtc = clock.UtcNow
                });
            }
        }
    }
}
