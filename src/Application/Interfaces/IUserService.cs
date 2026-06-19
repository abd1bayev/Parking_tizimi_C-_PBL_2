using Application.Common;
using Domain.Entities;

namespace Application.Interfaces;

public interface IUserService
{
    OperationResult<Vehicle> AddVehicle(Guid userId, string plateNumber, string model, string color);
    OperationResult<Reservation> CreateReservation(Guid userId, Guid vehicleId, string slotCode, DateTime reservedFromUtc, DateTime reservedToUtc);
    OperationResult CancelReservation(Guid userId, Guid reservationId);
    IReadOnlyList<Vehicle> GetUserVehicles(Guid userId);
    IReadOnlyList<Reservation> GetUserReservations(Guid userId);
    IReadOnlyList<Payment> GetUserPayments(Guid userId);
}
