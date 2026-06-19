using ParkingTizimi.Core.Services;
using ParkingTizimi.Domain.Entities;
using ParkingTizimi.Domain.Enums;

namespace ParkingTizimi.App;

public class ConsoleApp
{
    private readonly ParkingSystemService _service;

    public ConsoleApp(ParkingSystemService service)
    {
        _service = service;
    }

    public async Task RunAsync()
    {
        await _service.InitializeAsync();

        while (true)
        {
            PrintHeader();
            Console.WriteLine("1. Register");
            Console.WriteLine("2. Login");
            Console.WriteLine("3. Create Admin");
            Console.WriteLine("0. Exit");

            switch (ReadMenuChoice("Tanlov"))
            {
                case "1":
                    await RegisterAsync();
                    break;
                case "2":
                    await LoginAsync();
                    break;
                case "3":
                    await BootstrapAdminAsync();
                    break;
                case "0":
                    await _service.PersistAsync();
                    Console.WriteLine("Dastur yakunlandi.");
                    return;
                default:
                    PauseWithMessage("Noto'g'ri tanlov.");
                    break;
            }
        }
    }

    private async Task BootstrapAdminAsync()
    {
        if (_service.HasAdmin())
        {
            PauseWithMessage("Admin allaqachon mavjud. Yangi admin yaratib bo'lmaydi.");
            return;
        }

        var username = ReadRequired("Admin username");
        var password = ReadRequired("Admin password");
        var phoneNumber = ReadRequired("Admin telefon (+998XXXXXXXXX)");

        var result = _service.BootstrapAdmin(username, password, phoneNumber);
        if (result.Succeeded)
        {
            await _service.PersistAsync();
        }

        PauseWithMessage(result.Message);
    }

    private async Task RegisterAsync()
    {
        var username = ReadRequired("Username");
        var password = ReadRequired("Password");
        var phoneNumber = ReadRequired("Telefon (+998XXXXXXXXX)");

        var result = _service.RegisterUser(username, password, phoneNumber);
        if (result.Succeeded)
        {
            await _service.PersistAsync();
        }

        PauseWithMessage(result.Message);
    }

    private async Task LoginAsync()
    {
        var username = ReadRequired("Username");
        var password = ReadRequired("Password");
        var loginResult = _service.Login(username, password);

        if (!loginResult.Succeeded || loginResult.Value is null)
        {
            PauseWithMessage(loginResult.Message);
            return;
        }

        switch (loginResult.Value.Role)
        {
            case UserRole.Admin:
                await RunAdminMenuAsync(loginResult.Value);
                break;
            case UserRole.Operator:
                await RunOperatorMenuAsync(loginResult.Value);
                break;
            default:
                await RunUserMenuAsync(loginResult.Value);
                break;
        }
    }

    private async Task RunAdminMenuAsync(User admin)
    {
        while (true)
        {
            PrintHeader($"Admin: {admin.Username}");
            Console.WriteLine("1. Create Operator");
            Console.WriteLine("2. View Users");
            Console.WriteLine("3. View Slots");
            Console.WriteLine("4. View Reservations");
            Console.WriteLine("5. View Active Sessions");
            Console.WriteLine("0. Logout");

            switch (ReadMenuChoice("Tanlov"))
            {
                case "1":
                    await CreateOperatorAsync(admin);
                    break;
                case "2":
                    PrintUsers(_service.GetAllUsers());
                    Pause();
                    break;
                case "3":
                    PrintSlots();
                    Pause();
                    break;
                case "4":
                    PrintReservations(_service.GetAllReservations());
                    Pause();
                    break;
                case "5":
                    PrintSessions(_service.GetActiveSessions());
                    Pause();
                    break;
                case "0":
                    return;
                default:
                    PauseWithMessage("Noto'g'ri tanlov.");
                    break;
            }
        }
    }

    private async Task CreateOperatorAsync(User admin)
    {
        var username = ReadRequired("Operator username");
        var password = ReadRequired("Operator password");
        var phoneNumber = ReadRequired("Operator telefon (+998XXXXXXXXX)");
        var result = _service.CreateOperator(admin.Id, username, password, phoneNumber);

        if (result.Succeeded)
        {
            await _service.PersistAsync();
        }

        PauseWithMessage(result.Message);
    }

    private async Task RunOperatorMenuAsync(User operatorUser)
    {
        while (true)
        {
            PrintHeader($"Operator: {operatorUser.Username}");
            Console.WriteLine("1. Check In Vehicle");
            Console.WriteLine("2. Check Out Vehicle");
            Console.WriteLine("3. View Slots");
            Console.WriteLine("4. View Active Sessions");
            Console.WriteLine("0. Logout");

            switch (ReadMenuChoice("Tanlov"))
            {
                case "1":
                    await CheckInAsync(operatorUser);
                    break;
                case "2":
                    await CheckOutAsync(operatorUser);
                    break;
                case "3":
                    PrintSlots();
                    Pause();
                    break;
                case "4":
                    PrintSessions(_service.GetActiveSessions());
                    Pause();
                    break;
                case "0":
                    return;
                default:
                    PauseWithMessage("Noto'g'ri tanlov.");
                    break;
            }
        }
    }

    private async Task CheckInAsync(User operatorUser)
    {
        var username = ReadRequired("User username");
        var user = _service.FindUserByUsername(username);
        if (user is null)
        {
            PauseWithMessage("Foydalanuvchi topilmadi.");
            return;
        }

        var vehicles = _service.GetUserVehicles(user.Id);
        if (vehicles.Count == 0)
        {
            PauseWithMessage("Bu foydalanuvchida avtomobil mavjud emas.");
            return;
        }

        Console.WriteLine("Mavjud avtomobillar:");
        foreach (var vehicle in vehicles)
        {
            Console.WriteLine($"- {vehicle.Id} | {vehicle.PlateNumber} | {vehicle.Model}");
        }

        var vehicleIdText = ReadRequired("Vehicle Id");
        if (!Guid.TryParse(vehicleIdText, out var vehicleId))
        {
            PauseWithMessage("Vehicle Id noto'g'ri.");
            return;
        }

        PrintSlots();
        var slotCode = ReadRequired("Slot code");
        var result = _service.CheckIn(operatorUser.Id, user.Id, vehicleId, slotCode);

        if (result.Succeeded)
        {
            await _service.PersistAsync();
        }

        PauseWithMessage(result.Message);
    }

    private async Task CheckOutAsync(User operatorUser)
    {
        var sessions = _service.GetActiveSessions();
        if (sessions.Count == 0)
        {
            PauseWithMessage("Faol sessiyalar yo'q.");
            return;
        }

        PrintSessions(sessions);
        var sessionIdText = ReadRequired("Session Id");
        if (!Guid.TryParse(sessionIdText, out var sessionId))
        {
            PauseWithMessage("Session Id noto'g'ri.");
            return;
        }

        var result = _service.CheckOut(operatorUser.Id, sessionId);
        if (result.Succeeded)
        {
            await _service.PersistAsync();
        }

        PauseWithMessage(result.Message);
    }

    private async Task RunUserMenuAsync(User user)
    {
        while (true)
        {
            PrintHeader($"User: {user.Username}");
            Console.WriteLine("1. Add Vehicle");
            Console.WriteLine("2. View My Vehicles");
            Console.WriteLine("3. Create Reservation");
            Console.WriteLine("4. Cancel Reservation");
            Console.WriteLine("5. View My Reservations");
            Console.WriteLine("6. View My Payments");
            Console.WriteLine("0. Logout");

            switch (ReadMenuChoice("Tanlov"))
            {
                case "1":
                    await AddVehicleAsync(user);
                    break;
                case "2":
                    PrintVehicles(_service.GetUserVehicles(user.Id));
                    Pause();
                    break;
                case "3":
                    await CreateReservationAsync(user);
                    break;
                case "4":
                    await CancelReservationAsync(user);
                    break;
                case "5":
                    PrintReservations(_service.GetUserReservations(user.Id));
                    Pause();
                    break;
                case "6":
                    PrintPayments(_service.GetUserPayments(user.Id));
                    Pause();
                    break;
                case "0":
                    return;
                default:
                    PauseWithMessage("Noto'g'ri tanlov.");
                    break;
            }
        }
    }

    private async Task AddVehicleAsync(User user)
    {
        var plateNumber = ReadRequired("Plate number");
        var model = ReadRequired("Model");
        var color = ReadRequired("Color");

        var result = _service.AddVehicle(user.Id, plateNumber, model, color);
        if (result.Succeeded)
        {
            await _service.PersistAsync();
        }

        PauseWithMessage(result.Message);
    }

    private async Task CreateReservationAsync(User user)
    {
        var vehicles = _service.GetUserVehicles(user.Id);
        if (vehicles.Count == 0)
        {
            PauseWithMessage("Bron qilishdan oldin avtomobil qo'shing.");
            return;
        }

        PrintVehicles(vehicles);
        var vehicleIdText = ReadRequired("Vehicle Id");
        if (!Guid.TryParse(vehicleIdText, out var vehicleId))
        {
            PauseWithMessage("Vehicle Id noto'g'ri.");
            return;
        }

        PrintSlots();
        var slotCode = ReadRequired("Slot code");
        var reservedFromUtc = ReadDateTime("Bron boshlanishi (yyyy-MM-dd HH:mm)");
        var reservedToUtc = ReadDateTime("Bron tugashi (yyyy-MM-dd HH:mm)");
        var result = _service.CreateReservation(user.Id, vehicleId, slotCode, reservedFromUtc, reservedToUtc);

        if (result.Succeeded)
        {
            await _service.PersistAsync();
        }

        PauseWithMessage(result.Message);
    }

    private async Task CancelReservationAsync(User user)
    {
        var reservations = _service.GetUserReservations(user.Id);
        if (reservations.Count == 0)
        {
            PauseWithMessage("Bekor qilish uchun bron yo'q.");
            return;
        }

        PrintReservations(reservations);
        var reservationIdText = ReadRequired("Reservation Id");
        if (!Guid.TryParse(reservationIdText, out var reservationId))
        {
            PauseWithMessage("Reservation Id noto'g'ri.");
            return;
        }

        var result = _service.CancelReservation(user.Id, reservationId);
        if (result.Succeeded)
        {
            await _service.PersistAsync();
        }

        PauseWithMessage(result.Message);
    }

    private void PrintHeader(string? subtitle = null)
    {
        Console.Clear();
        Console.WriteLine("========================================");
        Console.WriteLine("       Parking Tizimi - C# Edition      ");
        Console.WriteLine("========================================");
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            Console.WriteLine(subtitle);
            Console.WriteLine("----------------------------------------");
        }
    }

    private void PrintUsers(IReadOnlyList<User> users)
    {
        Console.WriteLine("ID                                   | Username        | Role     | Phone");
        Console.WriteLine(new string('-', 88));
        foreach (var user in users)
        {
            Console.WriteLine($"{user.Id} | {user.Username,-15} | {user.Role,-8} | {user.PhoneNumber}");
        }
    }

    private void PrintVehicles(IReadOnlyList<Vehicle> vehicles)
    {
        Console.WriteLine("ID                                   | Plate      | Model           | Color");
        Console.WriteLine(new string('-', 88));
        foreach (var vehicle in vehicles)
        {
            Console.WriteLine($"{vehicle.Id} | {vehicle.PlateNumber,-10} | {vehicle.Model,-15} | {vehicle.Color}");
        }
    }

    private void PrintSlots()
    {
        Console.WriteLine("Code | Status      | Rate");
        Console.WriteLine(new string('-', 32));
        foreach (var slot in _service.GetSlots())
        {
            Console.WriteLine($"{slot.Code,-4} | {slot.Status,-11} | {slot.HourlyRate:N0} UZS");
        }
    }

    private void PrintReservations(IReadOnlyList<Reservation> reservations)
    {
        Console.WriteLine("ID                                   | Slot | Status     | From                | To");
        Console.WriteLine(new string('-', 110));
        foreach (var reservation in reservations)
        {
            var slot = _service.FindSlot(reservation.SlotId);
            Console.WriteLine($"{reservation.Id} | {slot?.Code ?? "N/A",-4} | {reservation.Status,-10} | {reservation.ReservedFromUtc:yyyy-MM-dd HH:mm} | {reservation.ReservedToUtc:yyyy-MM-dd HH:mm}");
        }
    }

    private void PrintSessions(IReadOnlyList<ParkingSession> sessions)
    {
        Console.WriteLine("ID                                   | User            | Slot | Check In");
        Console.WriteLine(new string('-', 100));
        foreach (var session in sessions)
        {
            var user = _service.FindUser(session.UserId);
            var slot = _service.FindSlot(session.SlotId);
            Console.WriteLine($"{session.Id} | {user?.Username ?? "N/A",-15} | {slot?.Code ?? "N/A",-4} | {session.CheckInUtc:yyyy-MM-dd HH:mm}");
        }
    }

    private void PrintPayments(IReadOnlyList<Payment> payments)
    {
        Console.WriteLine("ID                                   | Session Id                            | Amount     | Status");
        Console.WriteLine(new string('-', 110));
        foreach (var payment in payments)
        {
            Console.WriteLine($"{payment.Id} | {payment.SessionId} | {payment.Amount,10:N0} | {payment.Status}");
        }
    }

    private static string ReadMenuChoice(string label)
    {
        Console.Write($"{label}: ");
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    private static string ReadRequired(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            var value = Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            Console.WriteLine("Qiymat bo'sh bo'lmasligi kerak.");
        }
    }

    private static DateTime ReadDateTime(string label)
    {
        while (true)
        {
            var value = ReadRequired(label);
            if (DateTime.TryParse(value, out var parsed))
            {
                return DateTime.SpecifyKind(parsed, DateTimeKind.Local).ToUniversalTime();
            }

            Console.WriteLine("Sana formati noto'g'ri. Misol: 2026-06-19 14:30");
        }
    }

    private static void PauseWithMessage(string message)
    {
        Console.WriteLine();
        Console.WriteLine(message);
        Pause();
    }

    private static void Pause()
    {
        Console.WriteLine();
        Console.WriteLine("Davom etish uchun Enter bosing...");
        Console.ReadLine();
    }
}