using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ParkingTizimi.Core.Services;
using ParkingTizimi.Domain.Entities;
using ParkingTizimi.Domain.Enums;
using ParkingTizimi.Infrastructure.Repositories;
using ParkingTizimi.Shared.Results;
using ParkingTizimi.Shared.Time;

namespace ParkingTizimi.Desktop;

public partial class MainWindow : Window
{
    private readonly ParkingSystemService _service;
    private User? _currentUser;

    public MainWindow()
        : this(CreateDesignTimeService())
    {
    }

    public MainWindow(ParkingSystemService service)
    {
        _service = service;
        DesktopLog.Write("MainWindow constructor started.");
        InitializeComponent();
        RefreshUi();
        Opened += (_, _) => DesktopLog.Write("MainWindow Opened event fired.");
        Activated += (_, _) => DesktopLog.Write("MainWindow Activated event fired.");
        Closing += (_, _) => DesktopLog.Write("MainWindow Closing event fired.");
        DesktopLog.Write("MainWindow constructor completed.");
    }

    private static ParkingSystemService CreateDesignTimeService()
    {
        var service = new ParkingSystemService(new JsonParkingRepository(Directory.GetCurrentDirectory()), new SystemClock());
        Task.Run(() => service.InitializeAsync()).GetAwaiter().GetResult();
        return service;
    }

    private async void CreateAdminButton_Click(object? sender, RoutedEventArgs e)
    {
        var result = _service.BootstrapAdmin(AdminUsernameTextBox.Text ?? string.Empty, AdminPasswordTextBox.Text ?? string.Empty, AdminPhoneTextBox.Text ?? string.Empty);
        await HandleResultAsync(result, () =>
        {
            AdminUsernameTextBox.Text = string.Empty;
            AdminPasswordTextBox.Text = string.Empty;
            AdminPhoneTextBox.Text = string.Empty;
        });
    }

    private async void RegisterButton_Click(object? sender, RoutedEventArgs e)
    {
        var result = _service.RegisterUser(RegisterUsernameTextBox.Text ?? string.Empty, RegisterPasswordTextBox.Text ?? string.Empty, RegisterPhoneTextBox.Text ?? string.Empty);
        await HandleResultAsync(result, () =>
        {
            RegisterUsernameTextBox.Text = string.Empty;
            RegisterPasswordTextBox.Text = string.Empty;
            RegisterPhoneTextBox.Text = string.Empty;
        });
    }

    private async void LoginButton_Click(object? sender, RoutedEventArgs e)
    {
        var result = _service.Login(LoginUsernameTextBox.Text ?? string.Empty, LoginPasswordTextBox.Text ?? string.Empty);
        if (!result.Succeeded || result.Value is null)
        {
            ShowFeedback(result.Message, true);
            return;
        }

        _currentUser = result.Value;
        LoginPasswordTextBox.Text = string.Empty;
        await _service.PersistAsync();
        ShowFeedback(result.Message, false);
        RefreshUi();
    }

    private async void CreateOperatorButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentUser is null)
        {
            ShowFeedback("Admin login qilishi kerak.", true);
            return;
        }

        var result = _service.CreateOperator(_currentUser.Id, OperatorUsernameTextBox.Text ?? string.Empty, OperatorPasswordTextBox.Text ?? string.Empty, OperatorPhoneTextBox.Text ?? string.Empty);
        await HandleResultAsync(result, () =>
        {
            OperatorUsernameTextBox.Text = string.Empty;
            OperatorPasswordTextBox.Text = string.Empty;
            OperatorPhoneTextBox.Text = string.Empty;
        });
    }

    private async void AddVehicleButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentUser is null)
        {
            ShowFeedback("User login qilishi kerak.", true);
            return;
        }

        var result = _service.AddVehicle(_currentUser.Id, VehiclePlateTextBox.Text ?? string.Empty, VehicleModelTextBox.Text ?? string.Empty, VehicleColorTextBox.Text ?? string.Empty);
        await HandleResultAsync(result, () =>
        {
            VehiclePlateTextBox.Text = string.Empty;
            VehicleModelTextBox.Text = string.Empty;
            VehicleColorTextBox.Text = string.Empty;
        });
    }

    private async void CreateReservationButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentUser is null)
        {
            ShowFeedback("User login qilishi kerak.", true);
            return;
        }

        if (UserVehicleComboBox.SelectedItem is not SelectionItem vehicleOption)
        {
            ShowFeedback("Avtomobil tanlang.", true);
            return;
        }

        if (UserSlotComboBox.SelectedItem is not SelectionItem slotOption)
        {
            ShowFeedback("Slot tanlang.", true);
            return;
        }

        if (!TryParseLocalDateTime(ReservationStartTextBox.Text, out var startUtc) || !TryParseLocalDateTime(ReservationEndTextBox.Text, out var endUtc))
        {
            ShowFeedback("Sana formati noto'g'ri. Misol: 2026-06-19 14:00", true);
            return;
        }

        var result = _service.CreateReservation(_currentUser.Id, vehicleOption.Id, slotOption.Label, startUtc, endUtc);
        await HandleResultAsync(result);
    }

    private async void CancelReservationButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentUser is null)
        {
            ShowFeedback("User login qilishi kerak.", true);
            return;
        }

        if (UserReservationComboBox.SelectedItem is not SelectionItem reservationOption)
        {
            ShowFeedback("Reservation tanlang.", true);
            return;
        }

        var result = _service.CancelReservation(_currentUser.Id, reservationOption.Id);
        await HandleResultAsync(result);
    }

    private async void CheckInButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentUser is null)
        {
            ShowFeedback("Operator yoki admin login qilishi kerak.", true);
            return;
        }

        if (OperatorUserComboBox.SelectedItem is not SelectionItem userOption)
        {
            ShowFeedback("Foydalanuvchi tanlang.", true);
            return;
        }

        if (OperatorVehicleComboBox.SelectedItem is not SelectionItem vehicleOption)
        {
            ShowFeedback("Avtomobil tanlang.", true);
            return;
        }

        if (OperatorSlotComboBox.SelectedItem is not SelectionItem slotOption)
        {
            ShowFeedback("Slot tanlang.", true);
            return;
        }

        var result = _service.CheckIn(_currentUser.Id, userOption.Id, vehicleOption.Id, slotOption.Label);
        await HandleResultAsync(result);
    }

    private async void CheckOutButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentUser is null)
        {
            ShowFeedback("Operator yoki admin login qilishi kerak.", true);
            return;
        }

        if (OperatorSessionComboBox.SelectedItem is not SelectionItem sessionOption)
        {
            ShowFeedback("Faol session tanlang.", true);
            return;
        }

        var result = _service.CheckOut(_currentUser.Id, sessionOption.Id);
        await HandleResultAsync(result);
    }

    private void OperatorUserComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RefreshOperatorVehicleOptions();
    }

    private void LogoutButton_Click(object? sender, RoutedEventArgs e)
    {
        _currentUser = null;
        ShowFeedback("Session yakunlandi.", false);
        RefreshUi();
    }

    private async Task HandleResultAsync(OperationResult result, Action? onSuccess = null)
    {
        if (result.Succeeded)
        {
            onSuccess?.Invoke();
            await _service.PersistAsync();
            ShowFeedback(result.Message, false);
            RefreshUi();
            return;
        }

        ShowFeedback(result.Message, true);
    }

    private void RefreshUi()
    {
        RefreshOverview();
        RefreshSessionVisuals();
        RefreshAdminTab();
        RefreshOperatorTab();
        RefreshUserTab();
    }

    private void RefreshOverview()
    {
        var users = _service.GetAllUsers();
        var slots = _service.GetSlots();
        var reservations = _service.GetAllReservations();
        var sessions = _service.GetActiveSessions();

        OverviewUsersText.Text = $"Users: {users.Count}";
        OverviewSlotsText.Text = $"Slots: {slots.Count}";
        OverviewReservationsText.Text = $"Reservations: {reservations.Count}";
        OverviewSessionsText.Text = $"Active Sessions: {sessions.Count}";

        CardUsersText.Text = users.Count.ToString();
        CardAvailableSlotsText.Text = slots.Count(slot => slot.Status == SlotStatus.Available).ToString();
        CardSessionsText.Text = sessions.Count.ToString();

        OverviewSlotsListBox.ItemsSource = slots.Select(slot => $"{slot.Code} | {slot.Status} | {slot.HourlyRate:N0} UZS").ToList();

        var activity = new List<string>();
        activity.AddRange(reservations.Take(4).Select(reservation =>
        {
            var slot = _service.FindSlot(reservation.SlotId);
            var user = _service.FindUser(reservation.UserId);
            return $"Reservation | {user?.Username ?? "Unknown"} | {slot?.Code ?? "N/A"} | {reservation.Status}";
        }));
        activity.AddRange(sessions.Take(4).Select(session =>
        {
            var slot = _service.FindSlot(session.SlotId);
            var user = _service.FindUser(session.UserId);
            return $"Session | {user?.Username ?? "Unknown"} | {slot?.Code ?? "N/A"} | {session.CheckInUtc:yyyy-MM-dd HH:mm}";
        }));

        OverviewActivityListBox.ItemsSource = activity.Count > 0 ? activity : new[] { "Hozircha activity mavjud emas." };
    }

    private void RefreshSessionVisuals()
    {
        var isGuest = _currentUser is null;
        GuestTabs.IsVisible = isGuest;
        DashboardTabs.IsVisible = !isGuest;
        LogoutButton.IsVisible = !isGuest;

        CurrentUserText.Text = $"Foydalanuvchi: {_currentUser?.Username ?? "Guest"}";
        CurrentRoleText.Text = $"Rol: {_currentUser?.Role.ToString() ?? "Guest"}";
        RoleBadgeText.Text = _currentUser?.Role switch
        {
            UserRole.Admin => "Admin Panel",
            UserRole.Operator => "Operator Panel",
            UserRole.User => "User Panel",
            _ => "Guest Mode"
        };

        SystemHintText.Text = _currentUser?.Role switch
        {
            UserRole.Admin => "Operator yarating va umumiy parking holatini boshqaring.",
            UserRole.Operator => "Check-in va check-out oqimlarini bu paneldan boshqaring.",
            UserRole.User => "Avtomobil qo'shing, reservation qiling va to'lovlarni ko'ring.",
            _ => _service.HasAdmin() ? "Login yoki register qiling." : "Avval Create Admin orqali admin yarating."
        };

        var role = _currentUser?.Role;
        AdminTab.IsVisible = role == UserRole.Admin;
        OperatorTab.IsVisible = role is UserRole.Admin or UserRole.Operator;
        UserTab.IsVisible = role == UserRole.User;
    }

    private void RefreshAdminTab()
    {
        var users = _service.GetAllUsers();
        var slots = _service.GetSlots();
        var reservations = _service.GetAllReservations();
        var sessions = _service.GetActiveSessions();

        AdminUsersListBox.ItemsSource = users.Select(user => $"{user.Username} | {user.Role} | {user.PhoneNumber}").ToList();
        AdminSlotsListBox.ItemsSource = slots.Select(slot => $"{slot.Code} | {slot.Status} | {slot.HourlyRate:N0} UZS").ToList();
        AdminReservationsListBox.ItemsSource = reservations.Select(reservation =>
        {
            var slot = _service.FindSlot(reservation.SlotId);
            var user = _service.FindUser(reservation.UserId);
            return $"{user?.Username ?? "Unknown"} | {slot?.Code ?? "N/A"} | {reservation.Status}";
        }).ToList();
        AdminSessionsListBox.ItemsSource = sessions.Select(session =>
        {
            var slot = _service.FindSlot(session.SlotId);
            var user = _service.FindUser(session.UserId);
            return $"{user?.Username ?? "Unknown"} | {slot?.Code ?? "N/A"} | {session.CheckInUtc:yyyy-MM-dd HH:mm}";
        }).ToList();
    }

    private void RefreshOperatorTab()
    {
        var userOptions = _service.GetAllUsers()
            .Where(user => user.Role == UserRole.User)
            .Select(user => new SelectionItem(user.Id, $"{user.Username} | {user.PhoneNumber}"))
            .ToList();

        OperatorUserComboBox.ItemsSource = userOptions;
        if (OperatorUserComboBox.SelectedItem is not SelectionItem selectedUser || userOptions.All(option => option.Id != selectedUser.Id))
        {
            OperatorUserComboBox.SelectedIndex = userOptions.Count > 0 ? 0 : -1;
        }

        RefreshOperatorVehicleOptions();

        var slotOptions = _service.GetSlots()
            .Where(slot => slot.Status != SlotStatus.Occupied && slot.Status != SlotStatus.OutOfService)
            .Select(slot => new SelectionItem(slot.Id, slot.Code))
            .ToList();
        OperatorSlotComboBox.ItemsSource = slotOptions;
        if (slotOptions.Count > 0)
        {
            OperatorSlotComboBox.SelectedIndex = 0;
        }

        var sessions = _service.GetActiveSessions();
        var sessionOptions = sessions.Select(session =>
        {
            var user = _service.FindUser(session.UserId);
            var slot = _service.FindSlot(session.SlotId);
            return new SelectionItem(session.Id, $"{user?.Username ?? "Unknown"} | {slot?.Code ?? "N/A"} | {session.CheckInUtc:HH:mm}");
        }).ToList();
        OperatorSessionComboBox.ItemsSource = sessionOptions;
        if (sessionOptions.Count > 0)
        {
            OperatorSessionComboBox.SelectedIndex = 0;
        }

        OperatorSlotsListBox.ItemsSource = _service.GetSlots().Select(slot => $"{slot.Code} | {slot.Status} | {slot.HourlyRate:N0} UZS").ToList();
        OperatorSessionsListBox.ItemsSource = sessions.Select(session =>
        {
            var user = _service.FindUser(session.UserId);
            var slot = _service.FindSlot(session.SlotId);
            return $"{user?.Username ?? "Unknown"} | {slot?.Code ?? "N/A"} | {session.CheckInUtc:yyyy-MM-dd HH:mm}";
        }).ToList();
    }

    private void RefreshOperatorVehicleOptions()
    {
        if (OperatorUserComboBox.SelectedItem is not SelectionItem userOption)
        {
            OperatorVehicleComboBox.ItemsSource = Array.Empty<SelectionItem>();
            return;
        }

        var vehicles = _service.GetUserVehicles(userOption.Id)
            .Select(vehicle => new SelectionItem(vehicle.Id, $"{vehicle.PlateNumber} | {vehicle.Model}"))
            .ToList();
        OperatorVehicleComboBox.ItemsSource = vehicles;
        if (vehicles.Count > 0)
        {
            OperatorVehicleComboBox.SelectedIndex = 0;
        }
    }

    private void RefreshUserTab()
    {
        if (_currentUser is null)
        {
            UserVehicleComboBox.ItemsSource = Array.Empty<SelectionItem>();
            UserSlotComboBox.ItemsSource = Array.Empty<SelectionItem>();
            UserReservationComboBox.ItemsSource = Array.Empty<SelectionItem>();
            UserVehiclesListBox.ItemsSource = Array.Empty<string>();
            UserReservationsListBox.ItemsSource = Array.Empty<string>();
            UserPaymentsListBox.ItemsSource = Array.Empty<string>();
            return;
        }

        var vehicles = _service.GetUserVehicles(_currentUser.Id).ToList();
        var reservations = _service.GetUserReservations(_currentUser.Id).ToList();
        var payments = _service.GetUserPayments(_currentUser.Id).ToList();

        var vehicleOptions = vehicles.Select(vehicle => new SelectionItem(vehicle.Id, $"{vehicle.PlateNumber} | {vehicle.Model}"))
            .ToList();
        UserVehicleComboBox.ItemsSource = vehicleOptions;
        if (vehicleOptions.Count > 0)
        {
            UserVehicleComboBox.SelectedIndex = 0;
        }

        var slotOptions = _service.GetSlots()
            .Where(slot => slot.Status != SlotStatus.Occupied && slot.Status != SlotStatus.OutOfService)
            .Select(slot => new SelectionItem(slot.Id, slot.Code))
            .ToList();
        UserSlotComboBox.ItemsSource = slotOptions;
        if (slotOptions.Count > 0)
        {
            UserSlotComboBox.SelectedIndex = 0;
        }

        var reservationOptions = reservations
            .Where(reservation => reservation.Status == ReservationStatus.Active)
            .Select(reservation =>
            {
                var slot = _service.FindSlot(reservation.SlotId);
                return new SelectionItem(reservation.Id, $"{slot?.Code ?? "N/A"} | {reservation.ReservedFromUtc:MM-dd HH:mm} -> {reservation.ReservedToUtc:MM-dd HH:mm}");
            })
            .ToList();
        UserReservationComboBox.ItemsSource = reservationOptions;
        if (reservationOptions.Count > 0)
        {
            UserReservationComboBox.SelectedIndex = 0;
        }

        UserVehiclesListBox.ItemsSource = vehicles.Count > 0
            ? vehicles.Select(vehicle => $"{vehicle.PlateNumber} | {vehicle.Model} | {vehicle.Color}").ToList()
            : new[] { "Avtomobil mavjud emas." };

        UserReservationsListBox.ItemsSource = reservations.Count > 0
            ? reservations.Select(reservation =>
            {
                var slot = _service.FindSlot(reservation.SlotId);
                return $"{slot?.Code ?? "N/A"} | {reservation.Status} | {reservation.ReservedFromUtc:yyyy-MM-dd HH:mm}";
            }).ToList()
            : new[] { "Reservation mavjud emas." };

        UserPaymentsListBox.ItemsSource = payments.Count > 0
            ? payments.Select(payment => $"{payment.Amount:N0} UZS | {payment.Status} | {payment.PaidAtUtc:yyyy-MM-dd HH:mm}").ToList()
            : new[] { "Payment mavjud emas." };
    }

    private void ShowFeedback(string message, bool isError)
    {
        FeedbackText.Text = message;
        FeedbackText.Foreground = isError
            ? Avalonia.Media.Brushes.IndianRed
            : Avalonia.Media.Brushes.MediumSeaGreen;
    }

    private static bool TryParseLocalDateTime(string? value, out DateTime utcDateTime)
    {
        if (DateTime.TryParse(value, out var parsed))
        {
            utcDateTime = DateTime.SpecifyKind(parsed, DateTimeKind.Local).ToUniversalTime();
            return true;
        }

        utcDateTime = default;
        return false;
    }

    private sealed record SelectionItem(Guid Id, string Label)
    {
        public override string ToString() => Label;
    }
}