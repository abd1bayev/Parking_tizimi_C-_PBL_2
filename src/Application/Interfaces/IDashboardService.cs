using Application.DTOs.Dashboard;

namespace Application.Interfaces;

public interface IDashboardService
{
    DashboardOverviewDto GetOverview();
}
