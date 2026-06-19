namespace Domain.Entities;

public class ParkingSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid SlotId { get; set; }
    public DateTime CheckInUtc { get; set; }
    public DateTime? CheckOutUtc { get; set; }
    public bool IsClosed { get; set; }
    public decimal TotalAmount { get; set; }
}
