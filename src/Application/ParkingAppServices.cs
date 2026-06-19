using Application.Interfaces;

namespace Application;

public sealed class ParkingAppServices
{
    public ParkingAppServices(
        IParkingStateStore stateStore,
        IAuthService auth,
        IAdminService admin,
        IOperatorService operatorService,
        IUserService user,
        IProfileService profile,
        ICurrentUserService currentUser,
        IParkingQueryService query,
        IParkingMapService map,
        IDashboardService dashboard,
        IProblemReportService problems)
    {
        StateStore = stateStore;
        Auth = auth;
        Admin = admin;
        Operator = operatorService;
        User = user;
        Profile = profile;
        CurrentUser = currentUser;
        Query = query;
        Map = map;
        Dashboard = dashboard;
        Problems = problems;
    }

    public IParkingStateStore StateStore { get; }
    public IAuthService Auth { get; }
    public IAdminService Admin { get; }
    public IOperatorService Operator { get; }
    public IUserService User { get; }
    public IProfileService Profile { get; }
    public ICurrentUserService CurrentUser { get; }
    public IParkingQueryService Query { get; }
    public IParkingMapService Map { get; }
    public IDashboardService Dashboard { get; }
    public IProblemReportService Problems { get; }
}
