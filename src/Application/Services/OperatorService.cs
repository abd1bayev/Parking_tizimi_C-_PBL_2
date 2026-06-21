using Application.Common;
using Application.Interfaces;
using Application.Internal;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public sealed class OperatorService : IOperatorService
{
    private readonly IClock _clock;
    private readonly IParkingStateStore _stateStore;

    public OperatorService(IParkingStateStore stateStore, IClock clock)
    {
        _stateStore = stateStore;
        _clock = clock;
    }

    public OperationResult<ParkingSession> CheckIn(Guid operatorUserId, Guid userId, Guid vehicleId, string slotCode)
    {
        if (!ParkingStateHelper.IsOperator(_stateStore.State, operatorUserId))
        {
            return OperationResult<ParkingSession>.Failure("Faqat operator kirish qilishi mumkin.");
        }

        var user = ParkingStateHelper.GetActiveUser(_stateStore.State, userId);
        if (user is null || user.Role != UserRole.User)
        {
            return OperationResult<ParkingSession>.Failure("Foydalanuvchi topilmadi.");
        }

        var state = _stateStore.State;
        var vehicle = state.Vehicles.FirstOrDefault(candidate =>
            candidate.Id == vehicleId && candidate.OwnerUserId == userId);
        if (vehicle is null)
        {
            return OperationResult<ParkingSession>.Failure("Avtomobil topilmadi.");
        }

        if (state.Sessions.Any(session => session.VehicleId == vehicleId && !session.IsClosed))
        {
            return OperationResult<ParkingSession>.Failure("Bu avtomobil allaqachon parkingda.");
        }

        var slot = ParkingStateHelper.GetSlotByCode(state, slotCode);
        if (slot is null)
        {
            return OperationResult<ParkingSession>.Failure("Park joyi topilmadi.");
        }

        if (slot.Status is SlotStatus.Occupied or SlotStatus.OutOfService)
        {
            return OperationResult<ParkingSession>.Failure("Bu park joyiga kirish qilib bo'lmaydi.");
        }

        if (slot.Status == SlotStatus.Reserved)
        {
            var hasReservation = state.Reservations.Any(reservation =>
                reservation.SlotId == slot.Id &&
                reservation.UserId == userId &&
                reservation.VehicleId == vehicleId &&
                reservation.Status == ReservationStatus.Active);

            if (!hasReservation)
            {
                return OperationResult<ParkingSession>.Failure("Park joyi boshqa foydalanuvchi uchun bron qilingan.");
            }
        }

        foreach (var reservation in state.Reservations.Where(reservation =>
                     reservation.SlotId == slot.Id &&
                     reservation.UserId == userId &&
                     reservation.VehicleId == vehicleId &&
                     reservation.Status == ReservationStatus.Active))
        {
            reservation.Status = ReservationStatus.Completed;
        }

        var session = new ParkingSession
        {
            UserId = userId,
            VehicleId = vehicleId,
            SlotId = slot.Id,
            CheckInUtc = _clock.UtcNow,
            IsClosed = false,
            TotalAmount = 0m
        };

        state.Sessions.Add(session);
        ParkingStateHelper.RecalculateSlotStatus(state, slot.Id);

        return OperationResult<ParkingSession>.Success(session, "Kirish muvaffaqiyatli bajarildi.");
    }

    public OperationResult<Payment> CheckOut(Guid operatorUserId, Guid sessionId)
    {
        if (!ParkingStateHelper.IsOperator(_stateStore.State, operatorUserId))
        {
            return OperationResult<Payment>.Failure("Faqat operator chiqish qilishi mumkin.");
        }

        var state = _stateStore.State;
        var session = state.Sessions.FirstOrDefault(candidate => candidate.Id == sessionId && !candidate.IsClosed);
        if (session is null)
        {
            return OperationResult<Payment>.Failure("Faol park sessiyasi topilmadi.");
        }

        var slot = state.Slots.FirstOrDefault(candidate => candidate.Id == session.SlotId);
        if (slot is null)
        {
            return OperationResult<Payment>.Failure("Sessiya uchun park joyi topilmadi.");
        }

        var checkoutTime = _clock.UtcNow;
        var duration = checkoutTime - session.CheckInUtc;
        var billableHours = Math.Max(1, (int)Math.Ceiling(duration.TotalMinutes / 60d));
        var amount = billableHours * slot.HourlyRate;

        session.CheckOutUtc = checkoutTime;
        session.IsClosed = true;
        session.TotalAmount = amount;

        var payment = new Payment
        {
            SessionId = session.Id,
            Amount = amount,
            Status = PaymentStatus.Paid,
            CreatedAtUtc = checkoutTime,
            PaidAtUtc = checkoutTime
        };

        state.Payments.Add(payment);
        ParkingStateHelper.RecalculateSlotStatus(state, slot.Id);

        return OperationResult<Payment>.Success(payment, $"Chiqish yakunlandi. To'lov: {amount:N0} UZS");
    }
}
