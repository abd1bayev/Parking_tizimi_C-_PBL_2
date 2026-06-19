using Application.Common;
using Application.Interfaces;
using Application.Internal;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public sealed class UserService : IUserService
{
    private readonly IClock _clock;
    private readonly IParkingStateStore _stateStore;

    public UserService(IParkingStateStore stateStore, IClock clock)
    {
        _stateStore = stateStore;
        _clock = clock;
    }

    public OperationResult<Vehicle> AddVehicle(Guid userId, string plateNumber, string model, string color)
    {
        if (!ParkingStateHelper.IsEndUser(_stateStore.State, userId))
        {
            return OperationResult<Vehicle>.Failure("Faqat foydalanuvchi avtomobil qo'sha oladi.");
        }

        if (!InputNormalizer.TryNormalizeRequired(plateNumber, out var normalizedPlate))
        {
            return OperationResult<Vehicle>.Failure("Davlat raqami bo'sh bo'lmasligi kerak.");
        }

        if (!InputNormalizer.TryNormalizeRequired(model, out var normalizedModel))
        {
            return OperationResult<Vehicle>.Failure("Model bo'sh bo'lmasligi kerak.");
        }

        if (!InputNormalizer.TryNormalizeRequired(color, out var normalizedColor))
        {
            return OperationResult<Vehicle>.Failure("Rang bo'sh bo'lmasligi kerak.");
        }

        var state = _stateStore.State;
        normalizedPlate = normalizedPlate.ToUpperInvariant();
        if (state.Vehicles.Any(vehicle =>
                string.Equals(vehicle.PlateNumber, normalizedPlate, StringComparison.OrdinalIgnoreCase)))
        {
            return OperationResult<Vehicle>.Failure("Bunday davlat raqami allaqachon mavjud.");
        }

        var vehicle = new Vehicle
        {
            OwnerUserId = userId,
            PlateNumber = normalizedPlate,
            Model = normalizedModel,
            Color = normalizedColor,
            CreatedAtUtc = _clock.UtcNow
        };

        state.Vehicles.Add(vehicle);
        return OperationResult<Vehicle>.Success(vehicle, "Avtomobil qo'shildi.");
    }

    public OperationResult<Reservation> CreateReservation(
        Guid userId,
        Guid vehicleId,
        Guid zoneId,
        string slotCode,
        DateTime reservedFromUtc,
        DateTime reservedToUtc)
    {
        if (!ParkingStateHelper.IsEndUser(_stateStore.State, userId))
        {
            return OperationResult<Reservation>.Failure("Faqat foydalanuvchi bron qila oladi.");
        }

        var state = _stateStore.State;
        if (ParkingStateHelper.GetZoneById(state, zoneId) is null)
        {
            return OperationResult<Reservation>.Failure("Parking hududi topilmadi.");
        }

        var vehicle = state.Vehicles.FirstOrDefault(candidate =>
            candidate.Id == vehicleId && candidate.OwnerUserId == userId);
        if (vehicle is null)
        {
            return OperationResult<Reservation>.Failure("Avtomobil topilmadi.");
        }

        if (reservedToUtc <= reservedFromUtc)
        {
            return OperationResult<Reservation>.Failure("Bron vaqti noto'g'ri.");
        }

        if (reservedFromUtc < _clock.UtcNow.AddMinutes(-1))
        {
            return OperationResult<Reservation>.Failure("Bron vaqti o'tmishda bo'lmasligi kerak.");
        }

        var slot = ParkingStateHelper.GetSlotByCode(state, slotCode, zoneId);
        if (slot is null)
        {
            return OperationResult<Reservation>.Failure("Parking slot topilmadi.");
        }

        if (slot.Status is SlotStatus.Occupied or SlotStatus.OutOfService)
        {
            return OperationResult<Reservation>.Failure("Bu slot hozir bron uchun yaroqsiz.");
        }

        var hasOverlap = state.Reservations.Any(reservation =>
            reservation.SlotId == slot.Id &&
            reservation.Status == ReservationStatus.Active &&
            reservedFromUtc < reservation.ReservedToUtc &&
            reservation.ReservedFromUtc < reservedToUtc);

        if (hasOverlap)
        {
            return OperationResult<Reservation>.Failure("Bu slot ko'rsatilgan vaqtda bron qilingan.");
        }

        var reservation = new Reservation
        {
            UserId = userId,
            VehicleId = vehicleId,
            SlotId = slot.Id,
            ReservedFromUtc = reservedFromUtc,
            ReservedToUtc = reservedToUtc,
            Status = ReservationStatus.Active,
            CreatedAtUtc = _clock.UtcNow
        };

        state.Reservations.Add(reservation);
        ParkingStateHelper.RecalculateSlotStatus(state, slot.Id);

        return OperationResult<Reservation>.Success(reservation, "Bron yaratildi.");
    }

    public OperationResult CancelReservation(Guid userId, Guid reservationId)
    {
        if (!ParkingStateHelper.IsEndUser(_stateStore.State, userId))
        {
            return OperationResult.Failure("Faqat foydalanuvchi bronni bekor qila oladi.");
        }

        var state = _stateStore.State;
        var reservation = state.Reservations.FirstOrDefault(candidate =>
            candidate.Id == reservationId && candidate.UserId == userId);
        if (reservation is null)
        {
            return OperationResult.Failure("Bron topilmadi.");
        }

        if (reservation.Status != ReservationStatus.Active)
        {
            return OperationResult.Failure("Faqat faol bron bekor qilinadi.");
        }

        reservation.Status = ReservationStatus.Cancelled;
        ParkingStateHelper.RecalculateSlotStatus(state, reservation.SlotId);

        return OperationResult.Success("Bron bekor qilindi.");
    }

    public IReadOnlyList<Vehicle> GetUserVehicles(Guid userId) =>
        _stateStore.State.Vehicles
            .Where(vehicle => vehicle.OwnerUserId == userId)
            .OrderBy(vehicle => vehicle.PlateNumber)
            .ToList();

    public IReadOnlyList<Reservation> GetUserReservations(Guid userId) =>
        _stateStore.State.Reservations
            .Where(reservation => reservation.UserId == userId)
            .OrderByDescending(reservation => reservation.CreatedAtUtc)
            .ToList();

    public IReadOnlyList<Payment> GetUserPayments(Guid userId)
    {
        var state = _stateStore.State;
        var sessionIds = state.Sessions
            .Where(session => session.UserId == userId)
            .Select(session => session.Id)
            .ToHashSet();

        return state.Payments
            .Where(payment => sessionIds.Contains(payment.SessionId))
            .OrderByDescending(payment => payment.CreatedAtUtc)
            .ToList();
    }
}
