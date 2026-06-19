using Microsoft.Extensions.DependencyInjection;
using Application;
using Application.DTOs.Auth;
using Application.Interfaces;
using Domain.Enums;
using Infrastructure.DependencyInjection;
using Infrastructure.Time;

namespace Application.Tests;

public class ParkingApplicationTests
{
    private static async Task<ParkingAppServices> CreateAppAsync(string tempDir)
    {
        var services = new ServiceCollection();
        services.AddParkingInfrastructure(tempDir);
        var provider = services.BuildServiceProvider();
        var app = provider.GetRequiredService<ParkingAppServices>();
        await app.StateStore.InitializeAsync();
        return app;
    }

    [Fact]
    public async Task BootstrapAdmin_ShouldSucceed_WhenNoAdminExists()
    {
        var app = await CreateAppAsync(Path.GetTempPath());
        var result = app.Admin.BootstrapAdmin(new RegisterRequest
        {
            Username = "admin1",
            Password = "password123",
            PhoneNumber = "+998901234567"
        });

        Assert.True(result.Succeeded);
        Assert.Equal(UserRole.Admin, result.Value!.Role);
    }

    [Fact]
    public async Task Register_ShouldFail_WhenAdminDoesNotExist()
    {
        var app = await CreateAppAsync(Path.GetTempPath());
        var result = app.Auth.Register(new RegisterRequest
        {
            Username = "user1",
            Password = "password1",
            PhoneNumber = "+998901111111"
        });

        Assert.False(result.Succeeded);
        Assert.Contains("admin", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_ShouldSucceed_AfterAdminBootstrap()
    {
        var app = await CreateAppAsync(Path.GetTempPath());
        app.Admin.BootstrapAdmin(new RegisterRequest
        {
            Username = "admin",
            Password = "password123",
            PhoneNumber = "+998901234567"
        });

        var result = app.Auth.Register(new RegisterRequest
        {
            Username = "user1",
            Password = "password1",
            PhoneNumber = "+998901111111"
        });

        Assert.True(result.Succeeded);
        Assert.Equal(UserRole.User, result.Value!.Role);
    }

    [Fact]
    public async Task CheckIn_ShouldRejectAdmin_WhenAdminAttemptsOperationalCheckIn()
    {
        var app = await CreateAppAsync(Path.GetTempPath());
        var admin = app.Admin.BootstrapAdmin(new RegisterRequest
        {
            Username = "admin",
            Password = "password123",
            PhoneNumber = "+998901234567"
        }).Value!;

        app.Auth.Register(new RegisterRequest
        {
            Username = "user1",
            Password = "password1",
            PhoneNumber = "+998901111111"
        });

        var user = app.Query.FindUserByUsername("user1")!;
        var vehicle = app.User.AddVehicle(user.Id, "01A123BC", "Malibu", "Black").Value!;
        var zone = app.Map.GetAllZonesWithAvailability().First();

        var result = app.Operator.CheckIn(admin.UserId, user.Id, vehicle.Id, "CHZ-01");

        Assert.False(result.Succeeded);
        Assert.Contains("operator", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateOperator_ShouldRequireAdmin()
    {
        var app = await CreateAppAsync(Path.GetTempPath());
        app.Admin.BootstrapAdmin(new RegisterRequest
        {
            Username = "admin",
            Password = "password123",
            PhoneNumber = "+998901234567"
        });

        app.Auth.Register(new RegisterRequest
        {
            Username = "user1",
            Password = "password1",
            PhoneNumber = "+998901111111"
        });

        var user = app.Query.FindUserByUsername("user1")!;
        var result = app.Admin.CreateOperator(user.Id, new RegisterRequest
        {
            Username = "op1",
            Password = "password1",
            PhoneNumber = "+998902222222"
        });

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Profile_ShouldUpdatePhoneNumber()
    {
        var app = await CreateAppAsync(Path.GetTempPath());
        app.Admin.BootstrapAdmin(new RegisterRequest
        {
            Username = "admin",
            Password = "password123",
            PhoneNumber = "+998901234567"
        });

        var admin = app.Query.FindUserByUsername("admin")!;
        var result = app.Profile.UpdateProfile(new Application.DTOs.Profile.UpdateProfileRequest
        {
            UserId = admin.Id,
            PhoneNumber = "+998909999999"
        });

        Assert.True(result.Succeeded);
        Assert.Equal("+998909999999", result.Value!.PhoneNumber);
    }

    [Fact]
    public async Task Map_ShouldReturnNearbyZones()
    {
        var app = await CreateAppAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var zones = app.Map.SearchNearbyZones(new Application.DTOs.Map.NearbyZoneSearchRequest
        {
            Latitude = 41.2995,
            Longitude = 69.2401,
            RadiusKm = 5
        });

        Assert.NotEmpty(zones);
        Assert.All(zones, zone => Assert.True(zone.AvailableSlots >= 0));
    }
}

internal sealed class FakeClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2026, 6, 19, 10, 0, 0, DateTimeKind.Utc);
}
