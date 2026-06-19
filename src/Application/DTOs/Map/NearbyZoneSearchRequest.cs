namespace Application.DTOs.Map;

public sealed class NearbyZoneSearchRequest
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public double RadiusKm { get; init; } = 3;
}
