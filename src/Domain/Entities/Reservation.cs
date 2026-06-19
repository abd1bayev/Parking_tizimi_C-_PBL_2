using Domain.Enums;

namespace Domain.Entities;

public class Reservation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid SlotId { get; set; }
    public DateTime ReservedFromUtc { get; set; }
    public DateTime ReservedToUtc { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Active;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
