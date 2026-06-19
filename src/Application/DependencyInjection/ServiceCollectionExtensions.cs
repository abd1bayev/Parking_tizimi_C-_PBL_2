using Microsoft.Extensions.DependencyInjection;
using Application.Interfaces;
using Application.Services;

namespace Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddParkingApplication(this IServiceCollection services)
    {
        services.AddSingleton<IParkingStateStore, ParkingStateStore>();
        services.AddSingleton<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IAdminService, AdminService>();
        services.AddSingleton<IOperatorService, OperatorService>();
        services.AddSingleton<IUserService, UserService>();
        services.AddSingleton<IProfileService, ProfileService>();
        services.AddSingleton<IParkingQueryService, ParkingQueryService>();
        services.AddSingleton<IParkingMapService, ParkingMapService>();
        services.AddSingleton<IDashboardService, DashboardService>();
        services.AddSingleton<IProblemReportService, ProblemReportService>();
        services.AddSingleton<ParkingAppServices>();

        return services;
    }
}
