namespace Application.DTOs.Dashboard;

public sealed class DashboardOverviewDto
{
    public int TotalZones { get; init; }
    public int TotalSlots { get; init; }
    public int AvailableSlots { get; init; }
    public int OccupiedSlots { get; init; }
    public int ReservedSlots { get; init; }
    public int OutOfServiceSlots { get; init; }
    public int ActiveSessions { get; init; }
    public int ActiveReservations { get; init; }
    public int TotalUsers { get; init; }
    public int OpenProblems { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal TodayRevenue { get; init; }
    public double OccupancyPercent { get; init; }
    public IReadOnlyList<ZoneStatDto> ZoneStats { get; init; } = [];
    public IReadOnlyList<DailyRevenueDto> Last7DaysRevenue { get; init; } = [];
}

public sealed class ZoneStatDto
{
    public Guid ZoneId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Total { get; init; }
    public int Occupied { get; init; }
    public int Available { get; init; }
    public double OccupancyPercent { get; init; }
}

public sealed class DailyRevenueDto
{
    public DateOnly Date { get; init; }
    public decimal Amount { get; init; }
}
