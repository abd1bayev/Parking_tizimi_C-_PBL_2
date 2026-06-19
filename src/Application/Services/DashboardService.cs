using Application.DTOs.Dashboard;
using Application.Interfaces;
using Application.Internal;
using Application.Models;
using Domain.Enums;

namespace Application.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IClock _clock;
    private readonly IParkingStateStore _stateStore;

    public DashboardService(IParkingStateStore stateStore, IClock clock)
    {
        _stateStore = stateStore;
        _clock = clock;
    }

    public DashboardOverviewDto GetOverview()
    {
        var state = _stateStore.State;
        var slots = state.Slots;
        var today = DateOnly.FromDateTime(_clock.UtcNow);

        var zoneStats = state.Zones
            .Where(zone => zone.IsActive)
            .Select(zone =>
            {
                var zoneSlots = slots.Where(slot => slot.ZoneId == zone.Id).ToList();
                var occupied = zoneSlots.Count(s => s.Status == SlotStatus.Occupied);
                var total = zoneSlots.Count;
                return new ZoneStatDto
                {
                    ZoneId = zone.Id,
                    Code = zone.Code,
                    Name = zone.Name,
                    Total = total,
                    Occupied = occupied,
                    Available = zoneSlots.Count(s => s.Status == SlotStatus.Available),
                    OccupancyPercent = total == 0 ? 0 : Math.Round(occupied * 100d / total, 1)
                };
            })
            .OrderByDescending(stat => stat.OccupancyPercent)
            .ToList();

        var last7Days = Enumerable.Range(0, 7)
            .Select(offset => today.AddDays(-6 + offset))
            .Select(date => new DailyRevenueDto
            {
                Date = date,
                Amount = state.Payments
                    .Where(payment => payment.Status == PaymentStatus.Paid &&
                                      DateOnly.FromDateTime(payment.PaidAtUtc ?? payment.CreatedAtUtc) == date)
                    .Sum(payment => payment.Amount)
            })
            .ToList();

        var occupied = slots.Count(s => s.Status == SlotStatus.Occupied);
        var totalSlots = slots.Count;

        return new DashboardOverviewDto
        {
            TotalZones = state.Zones.Count(zone => zone.IsActive),
            TotalSlots = totalSlots,
            AvailableSlots = slots.Count(s => s.Status == SlotStatus.Available),
            OccupiedSlots = occupied,
            ReservedSlots = slots.Count(s => s.Status == SlotStatus.Reserved),
            OutOfServiceSlots = slots.Count(s => s.Status == SlotStatus.OutOfService),
            ActiveSessions = state.Sessions.Count(session => !session.IsClosed),
            ActiveReservations = state.Reservations.Count(r => r.Status == ReservationStatus.Active),
            TotalUsers = state.Users.Count(user => user.IsActive),
            OpenProblems = state.ProblemReports.Count(r => r.Status != ProblemStatus.Resolved),
            TotalRevenue = state.Payments
                .Where(payment => payment.Status == PaymentStatus.Paid)
                .Sum(payment => payment.Amount),
            TodayRevenue = last7Days.Last().Amount,
            OccupancyPercent = totalSlots == 0 ? 0 : Math.Round(occupied * 100d / totalSlots, 1),
            ZoneStats = zoneStats,
            Last7DaysRevenue = last7Days
        };
    }
}
