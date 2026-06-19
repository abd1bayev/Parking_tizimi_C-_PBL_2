namespace Application.DTOs.Map;

public sealed class ZoneAvailabilityDto
{
    public Guid ZoneId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string District { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public int TotalSlots { get; init; }
    public int AvailableSlots { get; init; }
    public int ReservedSlots { get; init; }
    public int OccupiedSlots { get; init; }
    public decimal HourlyRate { get; init; }
    public double? DistanceKm { get; init; }
}
