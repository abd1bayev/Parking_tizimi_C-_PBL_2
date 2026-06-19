using Microsoft.Extensions.DependencyInjection;
using Application.DependencyInjection;
using Application.Interfaces;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Infrastructure.Time;

namespace Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddParkingInfrastructure(this IServiceCollection services, string dataRootPath)
    {
        services.AddSingleton<IParkingRepository>(_ => new JsonParkingRepository(dataRootPath));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddParkingApplication();

        return services;
    }
}
