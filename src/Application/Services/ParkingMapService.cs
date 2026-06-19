using Application.DTOs.Map;
using Application.Interfaces;
using Application.Internal;
using Application.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public sealed class ParkingMapService : IParkingMapService
{
    private readonly IParkingStateStore _stateStore;

    public ParkingMapService(IParkingStateStore stateStore)
    {
        _stateStore = stateStore;
    }

    public IReadOnlyList<ZoneAvailabilityDto> GetAllZonesWithAvailability()
    {
        return _stateStore.State.Zones
            .Where(zone => zone.IsActive)
            .OrderBy(zone => zone.District)
            .ThenBy(zone => zone.Name)
            .Select(BuildZoneAvailability)
            .ToList();
    }

    public IReadOnlyList<ZoneAvailabilityDto> SearchNearbyZones(NearbyZoneSearchRequest request)
    {
        if (request.RadiusKm <= 0)
        {
            return [];
        }

        return GetAllZonesWithAvailability()
            .Select(zone =>
            {
                var distance = GeoHelper.DistanceKm(
                    request.Latitude, request.Longitude, zone.Latitude, zone.Longitude);
                return new ZoneAvailabilityDto
                {
                    ZoneId = zone.ZoneId,
                    Code = zone.Code,
                    Name = zone.Name,
                    District = zone.District,
                    Address = zone.Address,
                    Latitude = zone.Latitude,
                    Longitude = zone.Longitude,
                    TotalSlots = zone.TotalSlots,
                    AvailableSlots = zone.AvailableSlots,
                    ReservedSlots = zone.ReservedSlots,
                    OccupiedSlots = zone.OccupiedSlots,
                    HourlyRate = zone.HourlyRate,
                    DistanceKm = Math.Round(distance, 2)
                };
            })
            .Where(zone => zone.DistanceKm <= request.RadiusKm)
            .OrderBy(zone => zone.DistanceKm)
            .ToList();
    }

    public IReadOnlyList<ZoneSlotDto> GetZoneSlots(Guid zoneId, bool availableOnly = false)
    {
        var slots = _stateStore.State.Slots
            .Where(slot => slot.ZoneId == zoneId)
            .OrderBy(slot => slot.Code);

        if (availableOnly)
        {
            slots = slots.Where(slot => slot.Status == SlotStatus.Available).OrderBy(slot => slot.Code);
        }

        return slots.Select(slot => new ZoneSlotDto
        {
            SlotId = slot.Id,
            Code = slot.Code,
            Status = slot.Status,
            HourlyRate = slot.HourlyRate
        }).ToList();
    }

    public ZoneAvailabilityDto? GetZoneAvailability(Guid zoneId) =>
        GetAllZonesWithAvailability().FirstOrDefault(zone => zone.ZoneId == zoneId);

    private ZoneAvailabilityDto BuildZoneAvailability(ParkingZone zone)
    {
        var state = _stateStore.State;
        var slots = state.Slots.Where(slot => slot.ZoneId == zone.Id).ToList();
        var hourlyRate = slots.FirstOrDefault()?.HourlyRate ?? state.DefaultHourlyRate;

        return new ZoneAvailabilityDto
        {
            ZoneId = zone.Id,
            Code = zone.Code,
            Name = zone.Name,
            District = zone.District,
            Address = zone.Address,
            Latitude = zone.Latitude,
            Longitude = zone.Longitude,
            TotalSlots = slots.Count,
            AvailableSlots = slots.Count(s => s.Status == SlotStatus.Available),
            ReservedSlots = slots.Count(s => s.Status == SlotStatus.Reserved),
            OccupiedSlots = slots.Count(s => s.Status == SlotStatus.Occupied),
            HourlyRate = hourlyRate
        };
    }
}
