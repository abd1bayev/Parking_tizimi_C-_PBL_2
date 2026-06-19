using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public sealed class ParkingQueryService : IParkingQueryService
{
    private readonly IParkingStateStore _stateStore;

    public ParkingQueryService(IParkingStateStore stateStore)
    {
        _stateStore = stateStore;
    }

    public IReadOnlyList<ParkingSlot> GetSlots() =>
        _stateStore.State.Slots.OrderBy(slot => slot.Code).ToList();

    public IReadOnlyList<User> GetAllUsers() =>
        _stateStore.State.Users.OrderBy(user => user.Role).ThenBy(user => user.Username).ToList();

    public IReadOnlyList<Reservation> GetAllReservations() =>
        _stateStore.State.Reservations.OrderByDescending(r => r.CreatedAtUtc).ToList();

    public IReadOnlyList<ParkingSession> GetActiveSessions() =>
        _stateStore.State.Sessions.Where(s => !s.IsClosed).OrderBy(s => s.CheckInUtc).ToList();

    public IReadOnlyList<Payment> GetPayments() =>
        _stateStore.State.Payments.OrderByDescending(p => p.CreatedAtUtc).ToList();

    public User? FindUser(Guid userId) =>
        _stateStore.State.Users.FirstOrDefault(user => user.Id == userId);

    public User? FindUserByUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        return _stateStore.State.Users.FirstOrDefault(user =>
            string.Equals(user.Username, username.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public Vehicle? FindVehicle(Guid vehicleId) =>
        _stateStore.State.Vehicles.FirstOrDefault(vehicle => vehicle.Id == vehicleId);

    public ParkingSlot? FindSlot(Guid slotId) =>
        _stateStore.State.Slots.FirstOrDefault(slot => slot.Id == slotId);
}
