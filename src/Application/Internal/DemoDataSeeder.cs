using Application.Interfaces;
using Application.Models;
using Domain.Entities;
using Domain.Enums;

namespace Application.Internal;

internal static class DemoDataSeeder
{
    private const int ZoneCount = 120;
    private const int UserCount = 120;
    private const int ReservationCount = 150;
    private const int ProblemCount = 120;
    private const int ClosedSessionCount = 180;

    private static readonly string[] Districts =
    [
        "Chilonzor", "Yunusobod", "Mirzo Ulug'bek", "Yakkasaroy", "Sergeli", "Olmazor",
        "Uchtepa", "Bektemir", "Yashnobod", "Mirobod", "Shayxontohur", "Yangihayot"
    ];

    private static readonly string[] CarModels =
    [
        "Malibu", "Cobalt", "Spark", "Damass", "Gentra", "Nexia 3", "Tracker", "Onix",
        "Lacetti", "Captiva", "Matiz", "Monza", "Equinox", "Tico", "Jentra"
    ];

    private static readonly string[] Colors =
    [
        "Oq", "Qora", "Kumush", "Ko'k", "Qizil", "Yashil", "Kulrang", "Sariq"
    ];

    public static bool SeedIfEmpty(ParkingState state, IPasswordHasher passwordHasher, IClock clock)
    {
        if (Environment.GetEnvironmentVariable("PARKING_NO_DEMO") == "1")
        {
            return false;
        }

        if (state.Users.Count > 0)
        {
            return false;
        }

        state.Zones.Clear();
        state.Slots.Clear();
        state.Vehicles.Clear();
        state.Reservations.Clear();
        state.Sessions.Clear();
        state.Payments.Clear();
        state.ProblemReports.Clear();

        UserRegistration.CreateUser(state, passwordHasher, clock,
            "admin", "Admin123!", "+998901112233", UserRole.Admin, "Demo admin");
        UserRegistration.CreateUser(state, passwordHasher, clock,
            "operator", "Operator123!", "+998902223344", UserRole.Operator, "Demo operator");
        UserRegistration.CreateUser(state, passwordHasher, clock,
            "operator2", "Operator123!", "+998902223355", UserRole.Operator, "Demo operator 2");

        var users = CreateUsers(state, passwordHasher, clock);
        var vehicles = CreateVehicles(state, users, clock);
        GenerateZonesAndSlots(state, clock);
        SeedReservations(state, users, vehicles, clock);
        SeedActiveSessions(state, users, vehicles, clock);
        SeedClosedSessionsAndPayments(state, users, vehicles, clock);
        SeedProblemReports(state, users, clock);
        RecalculateAllSlotStatuses(state);

        return true;
    }

    private static List<User> CreateUsers(ParkingState state, IPasswordHasher passwordHasher, IClock clock)
    {
        var users = new List<User>(UserCount);

        UserRegistration.CreateUser(state, passwordHasher, clock,
            "demo_user", "User123!", "+998903334455", UserRole.User, "Demo user");
        users.Add(state.Users.Last(u => u.Username == "demo_user"));

        for (var i = 1; i <= UserCount - 1; i++)
        {
            var username = $"user_{i:D3}";
            var phone = $"+998901{i:D6}";

            var user = UserRegistration.CreateUser(
                state, passwordHasher, clock,
                username, "User123!", phone,
                UserRole.User, "Demo user").Value!;
            users.Add(user);
        }

        return users;
    }

    private static List<Vehicle> CreateVehicles(ParkingState state, List<User> users, IClock clock)
    {
        var vehicles = new List<Vehicle>();
        var plateIndex = 0;

        for (var i = 0; i < users.Count; i++)
        {
            var count = i % 5 == 0 ? 2 : 1;
            for (var v = 0; v < count; v++)
            {
                plateIndex++;
                vehicles.Add(new Vehicle
                {
                    OwnerUserId = users[i].Id,
                    PlateNumber = $"01A{plateIndex:D3}UZ",
                    Model = CarModels[(i + v) % CarModels.Length],
                    Color = Colors[(i + v) % Colors.Length],
                    CreatedAtUtc = clock.UtcNow.AddDays(-(i % 90 + 1))
                });
            }
        }

        state.Vehicles.AddRange(vehicles);
        return vehicles;
    }

    private static void GenerateZonesAndSlots(ParkingState state, IClock clock)
    {
        const double baseLat = 41.2995;
        const double baseLng = 69.2401;

        for (var i = 0; i < ZoneCount; i++)
        {
            var district = Districts[i % Districts.Length];
            var code = $"P{i + 1:D3}";
            var angle = i * 0.52;
            var radius = 0.02 + (i % 15) * 0.004;

            var zone = new ParkingZone
            {
                Code = code,
                Name = $"{district} parking #{i + 1}",
                District = district,
                Address = $"{district} tumani, {i + 1}-mavze",
                Latitude = baseLat + Math.Cos(angle) * radius,
                Longitude = baseLng + Math.Sin(angle) * radius,
                CreatedAtUtc = clock.UtcNow.AddDays(-(i % 30))
            };
            state.Zones.Add(zone);

            var hourlyRate = 4_000m + (i % 20) * 250m;
            var rows = 2 + (i % 3);
            const int cols = 4;

            for (var row = 0; row < rows; row++)
            {
                var rowLabel = (char)('A' + row);
                for (var col = 1; col <= cols; col++)
                {
                    state.Slots.Add(new ParkingSlot
                    {
                        ZoneId = zone.Id,
                        Code = $"{code}-{rowLabel}{col}",
                        Status = SlotStatus.Available,
                        HourlyRate = hourlyRate,
                        CreatedAtUtc = clock.UtcNow
                    });
                }
            }
        }
    }

    private static void SeedReservations(ParkingState state, List<User> users, List<Vehicle> vehicles, IClock clock)
    {
        var slots = state.Slots.OrderBy(s => s.Code).ToList();
        var slotIndex = 0;

        for (var i = 0; i < ReservationCount; i++)
        {
            var user = users[i % users.Count];
            var userVehicles = vehicles.Where(v => v.OwnerUserId == user.Id).ToList();
            if (userVehicles.Count == 0)
            {
                continue;
            }

            var vehicle = userVehicles[i % userVehicles.Count];
            var slot = slots[slotIndex % slots.Count];
            slotIndex += 7;

            var status = (i % 11) switch
            {
                0 => ReservationStatus.Cancelled,
                1 or 2 => ReservationStatus.Completed,
                _ => ReservationStatus.Active
            };

            var from = status == ReservationStatus.Active
                ? clock.UtcNow.AddHours(1 + (i % 48))
                : clock.UtcNow.AddDays(-(i % 14 + 1)).AddHours(i % 12);

            state.Reservations.Add(new Reservation
            {
                UserId = user.Id,
                VehicleId = vehicle.Id,
                SlotId = slot.Id,
                ReservedFromUtc = from,
                ReservedToUtc = from.AddHours(2 + (i % 4)),
                Status = status,
                CreatedAtUtc = from.AddHours(-2)
            });
        }
    }

    private static void SeedActiveSessions(ParkingState state, List<User> users, List<Vehicle> vehicles, IClock clock)
    {
        var slots = state.Slots.OrderBy(s => s.Code).ToList();
        var targetOccupied = (int)(slots.Count * 0.38);
        var vehicleIndex = 0;

        for (var i = 0; i < targetOccupied; i++)
        {
            var slot = slots[(i * 11) % slots.Count];
            if (state.Sessions.Any(s => s.SlotId == slot.Id && !s.IsClosed))
            {
                continue;
            }

            var user = users[i % users.Count];
            var userVehicles = vehicles.Where(v => v.OwnerUserId == user.Id).ToList();
            if (userVehicles.Count == 0)
            {
                continue;
            }

            var vehicle = userVehicles[vehicleIndex % userVehicles.Count];
            vehicleIndex++;

            if (state.Sessions.Any(s => s.VehicleId == vehicle.Id && !s.IsClosed))
            {
                continue;
            }

            state.Sessions.Add(new ParkingSession
            {
                UserId = user.Id,
                VehicleId = vehicle.Id,
                SlotId = slot.Id,
                CheckInUtc = clock.UtcNow.AddHours(-(1 + i % 8)),
                IsClosed = false,
                TotalAmount = 0m
            });
        }

        var outOfServiceCount = Math.Max(100, slots.Count / 15);
        for (var i = 0; i < outOfServiceCount; i++)
        {
            slots[(i * 13 + 5) % slots.Count].Status = SlotStatus.OutOfService;
        }
    }

    private static void SeedClosedSessionsAndPayments(
        ParkingState state,
        List<User> users,
        List<Vehicle> vehicles,
        IClock clock)
    {
        var slots = state.Slots.OrderBy(s => s.Code).ToList();

        for (var i = 0; i < ClosedSessionCount; i++)
        {
            var user = users[i % users.Count];
            var userVehicles = vehicles.Where(v => v.OwnerUserId == user.Id).ToList();
            if (userVehicles.Count == 0)
            {
                continue;
            }

            var vehicle = userVehicles[i % userVehicles.Count];
            var slot = slots[(i * 5) % slots.Count];
            var dayOffset = i % 7;
            var checkIn = clock.UtcNow.Date.AddDays(-dayOffset).AddHours(7 + (i % 12));
            var hours = 1 + (i % 5);
            var checkOut = checkIn.AddHours(hours);
            var amount = slot.HourlyRate * hours;

            var session = new ParkingSession
            {
                UserId = user.Id,
                VehicleId = vehicle.Id,
                SlotId = slot.Id,
                CheckInUtc = checkIn,
                CheckOutUtc = checkOut,
                IsClosed = true,
                TotalAmount = amount
            };
            state.Sessions.Add(session);
            state.Payments.Add(new Payment
            {
                SessionId = session.Id,
                Amount = amount,
                Status = PaymentStatus.Paid,
                CreatedAtUtc = checkOut,
                PaidAtUtc = checkOut
            });
        }
    }

    private static void SeedProblemReports(ParkingState state, List<User> users, IClock clock)
    {
        var titles = new[]
        {
            "Sensor ishlamayapti", "Yoritish yo'q", "Shlagbaum nosoz", "To'lov terminali xato",
            "Yo'l chizig'i o'chgan", "Kamera buzilgan", "Tozalash kerak", "Shovqin",
            "Yer o'quvchan", "Ventilyatsiya yo'q", "Wi-Fi ishlamaydi", "Display o'chiq"
        };

        for (var i = 0; i < ProblemCount; i++)
        {
            var zone = state.Zones[i % state.Zones.Count];
            var zoneSlots = state.Slots.Where(s => s.ZoneId == zone.Id).OrderBy(s => s.Code).ToList();
            var slotCode = i % 3 == 0 ? null : zoneSlots[i % zoneSlots.Count].Code;

            var status = (i % 12) switch
            {
                0 => ProblemStatus.Resolved,
                1 => ProblemStatus.InProgress,
                _ => ProblemStatus.Open
            };

            state.ProblemReports.Add(new ProblemReport
            {
                ReporterUserId = users[i % users.Count].Id,
                ZoneId = zone.Id,
                SlotCode = slotCode,
                Title = titles[i % titles.Length],
                Description = $"{zone.Name} — {titles[i % titles.Length]}. Tekshirish talab etiladi.",
                Status = status,
                CreatedAtUtc = clock.UtcNow.AddHours(-(i + 1)),
                ResolvedAtUtc = status == ProblemStatus.Resolved ? clock.UtcNow.AddHours(-i) : null
            });
        }
    }

    private static void RecalculateAllSlotStatuses(ParkingState state)
    {
        foreach (var slot in state.Slots)
        {
            if (slot.Status == SlotStatus.OutOfService)
            {
                continue;
            }

            ParkingStateHelper.RecalculateSlotStatus(state, slot.Id);
        }
    }
}
