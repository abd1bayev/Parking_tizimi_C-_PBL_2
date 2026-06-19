using ParkingTizimi.Core.Interfaces;
using ParkingTizimi.Core.Models;
using ParkingTizimi.Core.Services;
using ParkingTizimi.Domain.Enums;
using ParkingTizimi.Shared.Time;

namespace ParkingTizimi.Core.Tests;

public class ParkingSystemServiceTests
{
    [Fact]
    public async Task BootstrapAdmin_ShouldCreateAdmin_WhenNoneExists()
    {
        var service = await CreateServiceAsync();

        var result = service.BootstrapAdmin("admin_main", "secure_password_123", "+998901234567");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Value);
        Assert.Equal(UserRole.Admin, result.Value!.Role);
    }

    [Fact]
    public async Task RegisterUser_ShouldRejectInvalidPhoneNumber()
    {
        var service = await CreateServiceAsync();

        var result = service.RegisterUser("user1", "password1", "901234567");

        Assert.False(result.Succeeded);
        Assert.Contains("+998", result.Message);
    }

    [Fact]
    public async Task CreateReservation_ShouldReserveSlotForUserVehicle()
    {
        var service = await CreateServiceAsync();
        var admin = service.BootstrapAdmin("admin_main", "secure_password_123", "+998901234567").Value!;
        var user = service.RegisterUser("user1", "password1", "+998901111111").Value!;
        var vehicle = service.AddVehicle(user.Id, "01A123BC", "Malibu", "Black").Value!;

        var result = service.CreateReservation(user.Id, vehicle.Id, "A1", FixedClock.DefaultNow.AddHours(1), FixedClock.DefaultNow.AddHours(3));

        Assert.True(result.Succeeded);
        Assert.Equal("A1", service.FindSlot(result.Value!.SlotId)?.Code);
        Assert.Single(service.GetUserReservations(user.Id));
        Assert.Equal(SlotStatus.Reserved, service.GetSlots().First(slot => slot.Code == "A1").Status);
    }

    [Fact]
    public async Task CheckOut_ShouldCloseSessionAndCreatePayment()
    {
        var clock = new FixedClock(FixedClock.DefaultNow);
        var service = await CreateServiceAsync(clock);
        var admin = service.BootstrapAdmin("admin_main", "secure_password_123", "+998901234567").Value!;
        var operatorUser = service.CreateOperator(admin.Id, "operator1", "password1", "+998902222222").Value!;
        var user = service.RegisterUser("user1", "password1", "+998903333333").Value!;
        var vehicle = service.AddVehicle(user.Id, "01B777BC", "Cobalt", "White").Value!;

        var session = service.CheckIn(operatorUser.Id, user.Id, vehicle.Id, "A2").Value!;
        clock.Advance(TimeSpan.FromHours(2.2));

        var paymentResult = service.CheckOut(operatorUser.Id, session.Id);

        Assert.True(paymentResult.Succeeded);
        Assert.Equal(PaymentStatus.Paid, paymentResult.Value!.Status);
        Assert.Equal(15_000m, paymentResult.Value.Amount);
        Assert.Empty(service.GetActiveSessions());
        Assert.Equal(SlotStatus.Available, service.GetSlots().First(slot => slot.Code == "A2").Status);
    }

    private static async Task<ParkingSystemService> CreateServiceAsync(IClock? clock = null)
    {
        var repository = new InMemoryParkingRepository();
        var service = new ParkingSystemService(repository, clock ?? new FixedClock(FixedClock.DefaultNow));
        await service.InitializeAsync();
        return service;
    }

    private sealed class InMemoryParkingRepository : IParkingRepository
    {
        private ParkingState _state = new();

        public Task<ParkingState> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(_state);

        public Task SaveAsync(ParkingState state, CancellationToken cancellationToken = default)
        {
            _state = state;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock : IClock
    {
        public static readonly DateTime DefaultNow = new(2026, 6, 19, 10, 0, 0, DateTimeKind.Utc);

        private DateTime _utcNow;

        public FixedClock(DateTime utcNow)
        {
            _utcNow = utcNow;
        }

        public DateTime UtcNow => _utcNow;

        public void Advance(TimeSpan duration)
        {
            _utcNow = _utcNow.Add(duration);
        }
    }
}