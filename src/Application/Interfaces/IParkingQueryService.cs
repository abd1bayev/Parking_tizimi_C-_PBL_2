using Domain.Entities;

namespace Application.Interfaces;

public interface IParkingQueryService
{
    IReadOnlyList<ParkingSlot> GetSlots();
    IReadOnlyList<User> GetAllUsers();
    IReadOnlyList<Reservation> GetAllReservations();
    IReadOnlyList<ParkingSession> GetActiveSessions();
    IReadOnlyList<Payment> GetPayments();
    User? FindUser(Guid userId);
    User? FindUserByUsername(string username);
    Vehicle? FindVehicle(Guid vehicleId);
    ParkingSlot? FindSlot(Guid slotId);
}
