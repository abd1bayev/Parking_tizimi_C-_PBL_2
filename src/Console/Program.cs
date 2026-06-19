using Microsoft.Extensions.DependencyInjection;
using Application;
using Application.DTOs.Auth;
using Application.DTOs.Profile;
using Domain.Enums;
using Infrastructure.DependencyInjection;

namespace Cli;

internal static class Program
{
    private static async Task Main()
    {
        var rootPath = FindProjectRoot();
        var services = new ServiceCollection();
        services.AddParkingInfrastructure(rootPath);
        var provider = services.BuildServiceProvider();
        var app = provider.GetRequiredService<ParkingAppServices>();

        await app.StateStore.InitializeAsync();

        var console = new ConsoleApp(app);
        await console.RunAsync();
    }

    private static string FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ParkingTizimi.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}

internal sealed class ConsoleApp
{
    private readonly ParkingAppServices _app;

    public ConsoleApp(ParkingAppServices app) => _app = app;

    public async Task RunAsync()
    {
        System.Console.WriteLine("=== Parking Tizimi (Console) ===");
        System.Console.WriteLine("Ketma-ketlik: 1) Admin  2) Operator  3) User  4) Bron  5) Check-in/out");
        System.Console.WriteLine();

        while (true)
        {
            if (_app.CurrentUser.CurrentUser is null)
            {
                await ShowGuestMenuAsync();
            }
            else
            {
                await ShowRoleMenuAsync();
            }
        }
    }

    private async Task ShowGuestMenuAsync()
    {
        System.Console.WriteLine("\n--- Mehmon ---");
        System.Console.WriteLine("1. Birinchi admin yaratish (faqat bir marta)");
        System.Console.WriteLine("2. Login");
        if (_app.Auth.HasAdmin())
        {
            System.Console.WriteLine("3. Ro'yxatdan o'tish (User)");
        }

        System.Console.WriteLine("0. Chiqish");
        System.Console.Write("Tanlov: ");
        var choice = System.Console.ReadLine()?.Trim();

        switch (choice)
        {
            case "1":
                await BootstrapAdminAsync();
                break;
            case "2":
                await LoginAsync();
                break;
            case "3" when _app.Auth.HasAdmin():
                await RegisterAsync();
                break;
            case "0":
                Environment.Exit(0);
                break;
            default:
                System.Console.WriteLine("Noto'g'ri tanlov.");
                break;
        }
    }

    private async Task ShowRoleMenuAsync()
    {
        var user = _app.CurrentUser.CurrentUser!;
        System.Console.WriteLine($"\n--- {user.Role}: {user.Username} ---");

        switch (user.Role)
        {
            case UserRole.Admin:
                System.Console.WriteLine("1. Operator yaratish");
                System.Console.WriteLine("2. Barcha foydalanuvchilar");
                System.Console.WriteLine("3. Slotlar / bronlar / sessiyalar");
                System.Console.WriteLine("4. Profil");
                break;
            case UserRole.Operator:
                System.Console.WriteLine("1. Check-in");
                System.Console.WriteLine("2. Check-out");
                System.Console.WriteLine("3. Faol sessiyalar");
                System.Console.WriteLine("4. Slotlar");
                System.Console.WriteLine("5. Profil");
                break;
            case UserRole.User:
                System.Console.WriteLine("1. Avtomobil qo'shish");
                System.Console.WriteLine("2. Bron yaratish");
                System.Console.WriteLine("3. Bron bekor qilish");
                System.Console.WriteLine("4. Mening ma'lumotlarim");
                System.Console.WriteLine("5. Profil");
                break;
        }

        System.Console.WriteLine("9. Chiqish (logout)");
        System.Console.Write("Tanlov: ");

        var choice = System.Console.ReadLine()?.Trim();
        switch (user.Role)
        {
            case UserRole.Admin:
                await HandleAdminAsync(choice);
                break;
            case UserRole.Operator:
                await HandleOperatorAsync(choice);
                break;
            case UserRole.User:
                await HandleUserAsync(choice);
                break;
        }
    }

    private async Task BootstrapAdminAsync()
    {
        var request = ReadRegisterRequest();
        var result = _app.Admin.BootstrapAdmin(request);
        System.Console.WriteLine(result.Message);
        if (result.Succeeded)
        {
            await _app.StateStore.PersistAsync();
        }
    }

    private async Task RegisterAsync()
    {
        var request = ReadRegisterRequest();
        var result = _app.Auth.Register(request);
        System.Console.WriteLine(result.Message);
        if (result.Succeeded)
        {
            await _app.StateStore.PersistAsync();
        }
    }

    private async Task LoginAsync()
    {
        System.Console.Write("Username: ");
        var username = System.Console.ReadLine() ?? string.Empty;
        System.Console.Write("Parol: ");
        var password = ReadPassword();

        var result = _app.Auth.Login(new LoginRequest { Username = username, Password = password });
        System.Console.WriteLine(result.Message);

        if (result.Succeeded && result.Value is not null)
        {
            var user = _app.Query.FindUser(result.Value.UserId);
            _app.CurrentUser.SetCurrentUser(user);
            await _app.StateStore.PersistAsync();
        }
    }

    private async Task HandleAdminAsync(string? choice)
    {
        switch (choice)
        {
            case "1":
                var request = ReadRegisterRequest();
                var opResult = _app.Admin.CreateOperator(_app.CurrentUser.CurrentUser!.Id, request);
                System.Console.WriteLine(opResult.Message);
                if (opResult.Succeeded) await _app.StateStore.PersistAsync();
                break;
            case "2":
                foreach (var u in _app.Query.GetAllUsers())
                {
                    System.Console.WriteLine($"  [{u.Role}] {u.Username} — {u.PhoneNumber}");
                }
                break;
            case "3":
                PrintOverview();
                break;
            case "4":
                await ProfileMenuAsync();
                break;
            case "9":
                _app.CurrentUser.SignOut();
                break;
        }
    }

    private async Task HandleOperatorAsync(string? choice)
    {
        var operatorId = _app.CurrentUser.CurrentUser!.Id;
        switch (choice)
        {
            case "1":
                System.Console.Write("User ID: ");
                Guid.TryParse(System.Console.ReadLine(), out var userId);
                System.Console.Write("Vehicle ID: ");
                Guid.TryParse(System.Console.ReadLine(), out var vehicleId);
                System.Console.Write("Slot (masalan A1): ");
                var slot = System.Console.ReadLine() ?? string.Empty;
                var checkIn = _app.Operator.CheckIn(operatorId, userId, vehicleId, slot);
                System.Console.WriteLine(checkIn.Message);
                if (checkIn.Succeeded) await _app.StateStore.PersistAsync();
                break;
            case "2":
                System.Console.Write("Session ID: ");
                Guid.TryParse(System.Console.ReadLine(), out var sessionId);
                var checkOut = _app.Operator.CheckOut(operatorId, sessionId);
                System.Console.WriteLine(checkOut.Message);
                if (checkOut.Succeeded) await _app.StateStore.PersistAsync();
                break;
            case "3":
                foreach (var s in _app.Query.GetActiveSessions())
                {
                    System.Console.WriteLine($"  Session {s.Id} | User {s.UserId} | Slot {s.SlotId}");
                }
                break;
            case "4":
                foreach (var slotItem in _app.Query.GetSlots())
                {
                    System.Console.WriteLine($"  {slotItem.Code}: {slotItem.Status}");
                }
                break;
            case "5":
                await ProfileMenuAsync();
                break;
            case "9":
                _app.CurrentUser.SignOut();
                break;
        }
    }

    private async Task HandleUserAsync(string? choice)
    {
        var userId = _app.CurrentUser.CurrentUser!.Id;
        switch (choice)
        {
            case "1":
                System.Console.Write("Davlat raqami: ");
                var plate = System.Console.ReadLine() ?? string.Empty;
                System.Console.Write("Model: ");
                var model = System.Console.ReadLine() ?? string.Empty;
                System.Console.Write("Rang: ");
                var color = System.Console.ReadLine() ?? string.Empty;
                var vehicle = _app.User.AddVehicle(userId, plate, model, color);
                System.Console.WriteLine(vehicle.Message);
                if (vehicle.Succeeded) await _app.StateStore.PersistAsync();
                break;
            case "2":
                System.Console.Write("Vehicle ID: ");
                Guid.TryParse(System.Console.ReadLine(), out var vehicleId);
                System.Console.Write("Slot (A1): ");
                var slotCode = System.Console.ReadLine() ?? string.Empty;
                var from = DateTime.UtcNow.AddHours(1);
                var to = from.AddHours(2);
                var reservation = _app.User.CreateReservation(userId, vehicleId, slotCode, from, to);
                System.Console.WriteLine(reservation.Message);
                if (reservation.Succeeded) await _app.StateStore.PersistAsync();
                break;
            case "3":
                System.Console.Write("Reservation ID: ");
                Guid.TryParse(System.Console.ReadLine(), out var reservationId);
                var cancel = _app.User.CancelReservation(userId, reservationId);
                System.Console.WriteLine(cancel.Message);
                if (cancel.Succeeded) await _app.StateStore.PersistAsync();
                break;
            case "4":
                foreach (var v in _app.User.GetUserVehicles(userId))
                {
                    System.Console.WriteLine($"  Vehicle {v.Id}: {v.PlateNumber} ({v.Model})");
                }
                foreach (var r in _app.User.GetUserReservations(userId))
                {
                    System.Console.WriteLine($"  Reservation {r.Id}: slot {r.SlotId}, {r.Status}");
                }
                break;
            case "5":
                await ProfileMenuAsync();
                break;
            case "9":
                _app.CurrentUser.SignOut();
                break;
        }
    }

    private async Task ProfileMenuAsync()
    {
        var userId = _app.CurrentUser.CurrentUser!.Id;
        var profile = _app.Profile.GetProfile(userId);
        if (profile.Succeeded && profile.Value is not null)
        {
            System.Console.WriteLine($"Username: {profile.Value.Username}");
            System.Console.WriteLine($"Telefon: {profile.Value.PhoneNumber}");
            System.Console.WriteLine($"Rol: {profile.Value.Role}");
        }

        System.Console.Write("Yangi telefon (+998...): ");
        var phone = System.Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(phone))
        {
            var update = _app.Profile.UpdateProfile(new UpdateProfileRequest { UserId = userId, PhoneNumber = phone });
            System.Console.WriteLine(update.Message);
            if (update.Succeeded) await _app.StateStore.PersistAsync();
        }
    }

    private void PrintOverview()
    {
        System.Console.WriteLine($"Slotlar: {_app.Query.GetSlots().Count}");
        System.Console.WriteLine($"Bronlar: {_app.Query.GetAllReservations().Count}");
        System.Console.WriteLine($"Faol sessiyalar: {_app.Query.GetActiveSessions().Count}");
    }

    private static RegisterRequest ReadRegisterRequest()
    {
        System.Console.Write("Username: ");
        var username = System.Console.ReadLine() ?? string.Empty;
        System.Console.Write("Parol: ");
        var password = ReadPassword();
        System.Console.Write("Telefon (+998XXXXXXXXX): ");
        var phone = System.Console.ReadLine() ?? string.Empty;
        return new RegisterRequest { Username = username, Password = password, PhoneNumber = phone };
    }

    private static string ReadPassword()
    {
        var password = string.Empty;
        ConsoleKeyInfo key;
        do
        {
            key = System.Console.ReadKey(true);
            if (key.Key is ConsoleKey.Backspace or ConsoleKey.Delete)
            {
                if (password.Length > 0)
                {
                    password = password[..^1];
                }
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password += key.KeyChar;
            }
        } while (key.Key != ConsoleKey.Enter);

        System.Console.WriteLine();
        return password;
    }
}
