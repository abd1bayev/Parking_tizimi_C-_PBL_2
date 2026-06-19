using Application.DTOs.Map;

namespace Application.Interfaces;

public interface IParkingMapService
{
    IReadOnlyList<ZoneAvailabilityDto> GetAllZonesWithAvailability();
    IReadOnlyList<ZoneAvailabilityDto> SearchNearbyZones(NearbyZoneSearchRequest request);
    IReadOnlyList<ZoneSlotDto> GetZoneSlots(Guid zoneId, bool availableOnly = false);
    ZoneAvailabilityDto? GetZoneAvailability(Guid zoneId);
}
