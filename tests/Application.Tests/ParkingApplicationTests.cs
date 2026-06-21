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
    private static async Task<ParkingAppServices> CreateAppAsync(string tempDir, bool allowDemo = false)
    {
        Environment.SetEnvironmentVariable("PARKING_NO_DEMO", allowDemo ? null : "1");

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
        Assert.Contains("ma'mur", result.Message, StringComparison.OrdinalIgnoreCase);
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

        var result = app.Operator.CheckIn(admin.UserId, user.Id, vehicle.Id, "CHZ-A1");

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
    public async Task CancelReservation_ShouldFreeSlot()
    {
        var app = await CreateAppAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
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
        var vehicle = app.User.AddVehicle(user.Id, "01A123BC", "Malibu", "Black").Value!;
        var zone = app.Map.GetAllZonesWithAvailability().First();
        var slot = app.Map.GetZoneSlots(zone.ZoneId, availableOnly: true).First();

        var reservation = app.User.CreateReservation(
            user.Id, vehicle.Id, zone.ZoneId, slot.Code,
            DateTime.UtcNow.AddHours(1), DateTime.UtcNow.AddHours(3)).Value!;

        var cancel = app.User.CancelReservation(user.Id, reservation.Id);
        Assert.True(cancel.Succeeded);
        Assert.Equal(SlotStatus.Available, app.Query.FindSlot(slot.SlotId)!.Status);
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

    [Fact]
    public async Task DemoSeed_ShouldCreateLargeDataset()
    {
        var app = await CreateAppAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")), allowDemo: true);
        Assert.True(app.Auth.HasAdmin());
        Assert.True(app.Query.GetAllUsers().Count(u => u.Role == UserRole.User) >= 100);

        var overview = app.Dashboard.GetOverview();
        Assert.True(overview.TotalZones >= 100);
        Assert.True(overview.TotalSlots >= 100);
        Assert.True(overview.ActiveReservations >= 100);
        Assert.True(overview.OpenProblems >= 100);
        Assert.True(overview.OccupiedSlots >= 100);
        Assert.True(app.Query.GetPayments().Count >= 100);
    }

    [Fact]
    public async Task ProblemReport_ShouldBeStored()
    {
        var app = await CreateAppAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var zone = app.Map.GetAllZonesWithAvailability().First();
        var result = app.Problems.Report(new Application.DTOs.Problems.ReportProblemRequest
        {
            ZoneId = zone.ZoneId,
            Title = "Test muammo",
            Description = "Sensor ishlamayapti"
        });

        Assert.True(result.Succeeded);
        Assert.Single(app.Problems.GetOpenReports());
    }

    [Fact]
    public async Task CheckOut_ShouldCalculateBilling()
    {
        var app = await CreateAppAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        app.Admin.BootstrapAdmin(new RegisterRequest
        {
            Username = "admin",
            Password = "password123",
            PhoneNumber = "+998901234567"
        });
        app.Admin.CreateOperator(app.Query.FindUserByUsername("admin")!.Id, new RegisterRequest
        {
            Username = "operator",
            Password = "password1",
            PhoneNumber = "+998902222222"
        });
        app.Auth.Register(new RegisterRequest
        {
            Username = "user1",
            Password = "password1",
            PhoneNumber = "+998901111111"
        });

        var operatorUser = app.Query.FindUserByUsername("operator")!;
        var user = app.Query.FindUserByUsername("user1")!;
        var vehicle = app.User.AddVehicle(user.Id, "01A123BC", "Malibu", "Black").Value!;
        var slot = app.Map.GetZoneSlots(app.Map.GetAllZonesWithAvailability().First().ZoneId).First(s => s.Status == SlotStatus.Available);

        var session = app.Operator.CheckIn(operatorUser.Id, user.Id, vehicle.Id, slot.Code).Value!;
        var payment = app.Operator.CheckOut(operatorUser.Id, session.Id);

        Assert.True(payment.Succeeded);
        Assert.True(payment.Value!.Amount >= 5_000m);
    }

    [Fact]
    public async Task Dashboard_ShouldReturnOverview()
    {
        var app = await CreateAppAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var overview = app.Dashboard.GetOverview();
        Assert.True(overview.TotalZones >= 6);
        Assert.True(overview.TotalSlots >= 60);
    }
}

internal sealed class FakeClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2026, 6, 19, 10, 0, 0, DateTimeKind.Utc);
}
