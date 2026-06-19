using Domain.Enums;

namespace Application.DTOs.Map;

public sealed class ZoneSlotDto
{
    public Guid SlotId { get; init; }
    public string Code { get; init; } = string.Empty;
    public SlotStatus Status { get; init; }
    public decimal HourlyRate { get; init; }
}
