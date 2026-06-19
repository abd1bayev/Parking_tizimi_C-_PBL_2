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

    public static ParkingSlot? GetSlotByCode(ParkingState state, string slotCode)
    {
        if (!InputNormalizer.TryNormalizeRequired(slotCode, out var normalizedCode))
        {
            return null;
        }

        normalizedCode = normalizedCode.ToUpperInvariant();
        return state.Slots.FirstOrDefault(slot =>
            string.Equals(slot.Code, normalizedCode, StringComparison.OrdinalIgnoreCase));
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

    public static void SeedSlotsIfNeeded(ParkingState state, IClock clock)
    {
        if (state.Slots.Count > 0)
        {
            return;
        }

        foreach (var prefix in new[] { "A", "B" })
        {
            for (var number = 1; number <= 5; number++)
            {
                state.Slots.Add(new ParkingSlot
                {
                    Code = $"{prefix}{number}",
                    Status = SlotStatus.Available,
                    HourlyRate = state.DefaultHourlyRate,
                    CreatedAtUtc = clock.UtcNow
                });
            }
        }
    }
}
