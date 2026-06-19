using Domain.Entities;

namespace Application.Models;

public class ParkingState
{
    public List<User> Users { get; set; } = [];
    public List<Vehicle> Vehicles { get; set; } = [];
    public List<ParkingZone> Zones { get; set; } = [];
    public List<ParkingSlot> Slots { get; set; } = [];
    public List<Reservation> Reservations { get; set; } = [];
    public List<ParkingSession> Sessions { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
    public decimal DefaultHourlyRate { get; set; } = 5_000m;
}
