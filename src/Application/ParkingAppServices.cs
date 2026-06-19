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
        IParkingQueryService query)
    {
        StateStore = stateStore;
        Auth = auth;
        Admin = admin;
        Operator = operatorService;
        User = user;
        Profile = profile;
        CurrentUser = currentUser;
        Query = query;
    }

    public IParkingStateStore StateStore { get; }
    public IAuthService Auth { get; }
    public IAdminService Admin { get; }
    public IOperatorService Operator { get; }
    public IUserService User { get; }
    public IProfileService Profile { get; }
    public ICurrentUserService CurrentUser { get; }
    public IParkingQueryService Query { get; }
}
