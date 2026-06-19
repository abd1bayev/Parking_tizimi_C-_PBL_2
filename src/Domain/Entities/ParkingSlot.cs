using Domain.Enums;

namespace Domain.Entities;

public class ParkingSlot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ZoneId { get; set; }
    public string Code { get; set; } = string.Empty;
    public SlotStatus Status { get; set; } = SlotStatus.Available;
    public decimal HourlyRate { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
