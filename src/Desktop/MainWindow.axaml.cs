using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Application;
using Application.DTOs.Auth;
using Application.DTOs.Profile;
using Domain.Enums;

namespace Desktop;

public partial class MainWindow : Window
{
    private readonly ParkingAppServices _app;

    public MainWindow()
        : this(App.Services)
    {
    }

    public MainWindow(ParkingAppServices app)
    {
        _app = app;
        InitializeComponent();
        RefreshGuestState();
    }

    private void RefreshGuestState()
    {
        var hasAdmin = _app.Auth.HasAdmin();
        BootstrapPanel.IsVisible = !hasAdmin;
        RegisterPanel.IsVisible = hasAdmin;
    }

    private async Task PersistAsync()
    {
        await _app.StateStore.PersistAsync();
    }

    private void SetStatus(string message) => StatusText.Text = message;

    private async void BootstrapAdmin_Click(object? sender, RoutedEventArgs e)
    {
        var result = _app.Admin.BootstrapAdmin(new RegisterRequest
        {
            Username = AdminUsernameBox.Text ?? string.Empty,
            Password = AdminPasswordBox.Text ?? string.Empty,
            PhoneNumber = AdminPhoneBox.Text ?? string.Empty
        });

        SetStatus(result.Message);
        if (result.Succeeded)
        {
            await PersistAsync();
            RefreshGuestState();
        }
    }

    private async void LoginButton_Click(object? sender, RoutedEventArgs e)
    {
        var result = _app.Auth.Login(new LoginRequest
        {
            Username = LoginUsernameBox.Text ?? string.Empty,
            Password = LoginPasswordBox.Text ?? string.Empty
        });

        SetStatus(result.Message);
        if (!result.Succeeded || result.Value is null)
        {
            return;
        }

        var user = _app.Query.FindUser(result.Value.UserId);
        _app.CurrentUser.SetCurrentUser(user);
        await PersistAsync();
        ApplyRoleUi();
    }

    private async void RegisterButton_Click(object? sender, RoutedEventArgs e)
    {
        var result = _app.Auth.Register(new RegisterRequest
        {
            Username = RegisterUsernameBox.Text ?? string.Empty,
            Password = RegisterPasswordBox.Text ?? string.Empty,
            PhoneNumber = RegisterPhoneBox.Text ?? string.Empty
        });

        SetStatus(result.Message);
        if (result.Succeeded)
        {
            await PersistAsync();
        }
    }

    private void LogoutButton_Click(object? sender, RoutedEventArgs e)
    {
        _app.CurrentUser.SignOut();
        ApplyRoleUi();
        RefreshGuestState();
        SetStatus("Chiqildi.");
    }

    private void ApplyRoleUi()
    {
        var user = _app.CurrentUser.CurrentUser;
        var isLoggedIn = user is not null;

        GuestPanel.IsVisible = !isLoggedIn;
        LogoutButton.IsVisible = isLoggedIn;
        ProfilePanel.IsVisible = isLoggedIn;

        AdminPanel.IsVisible = user?.Role == UserRole.Admin;
        OperatorPanel.IsVisible = user?.Role == UserRole.Operator;
        UserPanel.IsVisible = user?.Role == UserRole.User;

        UserInfoText.Text = isLoggedIn
            ? $"{user!.Role}\n{user.Username}\n{user.PhoneNumber}"
            : "Mehmon";

        if (isLoggedIn)
        {
            var profile = _app.Profile.GetProfile(user!.Id);
            if (profile.Succeeded && profile.Value is not null)
            {
                ProfileInfoText.Text =
                    $"Username: {profile.Value.Username}\nRol: {profile.Value.Role}\nTelefon: {profile.Value.PhoneNumber}";
            }
        }
    }

    private async void CreateOperator_Click(object? sender, RoutedEventArgs e)
    {
        if (_app.CurrentUser.CurrentUser?.Role != UserRole.Admin)
        {
            SetStatus("Faqat admin operator yarata oladi.");
            return;
        }

        var result = _app.Admin.CreateOperator(_app.CurrentUser.CurrentUser.Id, new RegisterRequest
        {
            Username = OperatorUsernameBox.Text ?? string.Empty,
            Password = OperatorPasswordBox.Text ?? string.Empty,
            PhoneNumber = OperatorPhoneBox.Text ?? string.Empty
        });

        SetStatus(result.Message);
        if (result.Succeeded)
        {
            await PersistAsync();
        }
    }

    private void RefreshUsers_Click(object? sender, RoutedEventArgs e)
    {
        UsersListBox.ItemsSource = _app.Query.GetAllUsers()
            .Select(u => $"[{u.Role}] {u.Username} — {u.PhoneNumber}")
            .ToList();
    }

    private void RefreshOverview_Click(object? sender, RoutedEventArgs e)
    {
        OverviewText.Text =
            $"Slotlar: {_app.Query.GetSlots().Count} | " +
            $"Bronlar: {_app.Query.GetAllReservations().Count} | " +
            $"Faol sessiyalar: {_app.Query.GetActiveSessions().Count} | " +
            $"To'lovlar: {_app.Query.GetPayments().Count}";
    }

    private async void CheckIn_Click(object? sender, RoutedEventArgs e)
    {
        if (_app.CurrentUser.CurrentUser?.Role != UserRole.Operator)
        {
            SetStatus("Faqat operator check-in qila oladi.");
            return;
        }

        Guid.TryParse(CheckInUserIdBox.Text, out var userId);
        Guid.TryParse(CheckInVehicleIdBox.Text, out var vehicleId);

        var result = _app.Operator.CheckIn(
            _app.CurrentUser.CurrentUser.Id,
            userId,
            vehicleId,
            CheckInSlotBox.Text ?? string.Empty);

        SetStatus(result.Message);
        if (result.Succeeded)
        {
            await PersistAsync();
        }
    }

    private async void CheckOut_Click(object? sender, RoutedEventArgs e)
    {
        if (_app.CurrentUser.CurrentUser?.Role != UserRole.Operator)
        {
            SetStatus("Faqat operator check-out qila oladi.");
            return;
        }

        Guid.TryParse(CheckOutSessionIdBox.Text, out var sessionId);
        var result = _app.Operator.CheckOut(_app.CurrentUser.CurrentUser.Id, sessionId);
        SetStatus(result.Message);
        if (result.Succeeded)
        {
            await PersistAsync();
        }
    }

    private void RefreshSessions_Click(object? sender, RoutedEventArgs e)
    {
        SessionsListBox.ItemsSource = _app.Query.GetActiveSessions()
            .Select(s => $"Session {s.Id} | User {s.UserId} | Slot {s.SlotId} | {s.CheckInUtc:g}")
            .ToList();
    }

    private async void AddVehicle_Click(object? sender, RoutedEventArgs e)
    {
        var user = _app.CurrentUser.CurrentUser;
        if (user?.Role != UserRole.User)
        {
            SetStatus("Faqat foydalanuvchi avtomobil qo'sha oladi.");
            return;
        }

        var result = _app.User.AddVehicle(
            user.Id,
            PlateBox.Text ?? string.Empty,
            ModelBox.Text ?? string.Empty,
            ColorBox.Text ?? string.Empty);

        SetStatus(result.Message);
        if (result.Succeeded)
        {
            await PersistAsync();
        }
    }

    private async void CreateReservation_Click(object? sender, RoutedEventArgs e)
    {
        var user = _app.CurrentUser.CurrentUser;
        if (user?.Role != UserRole.User)
        {
            SetStatus("Faqat foydalanuvchi bron qila oladi.");
            return;
        }

        Guid.TryParse(ReservationVehicleIdBox.Text, out var vehicleId);
        var from = DateTime.UtcNow.AddHours(1);
        var to = from.AddHours(2);

        var result = _app.User.CreateReservation(
            user.Id,
            vehicleId,
            ReservationSlotBox.Text ?? string.Empty,
            from,
            to);

        SetStatus(result.Message);
        if (result.Succeeded)
        {
            await PersistAsync();
        }
    }

    private void RefreshUserData_Click(object? sender, RoutedEventArgs e)
    {
        var user = _app.CurrentUser.CurrentUser;
        if (user is null)
        {
            return;
        }

        var items = new List<string>();
        foreach (var v in _app.User.GetUserVehicles(user.Id))
        {
            items.Add($"Vehicle {v.Id}: {v.PlateNumber} ({v.Model}, {v.Color})");
        }

        foreach (var r in _app.User.GetUserReservations(user.Id))
        {
            items.Add($"Reservation {r.Id}: slot {r.SlotId}, {r.Status}");
        }

        UserDataListBox.ItemsSource = items;
    }

    private async void UpdateProfile_Click(object? sender, RoutedEventArgs e)
    {
        var user = _app.CurrentUser.CurrentUser;
        if (user is null)
        {
            return;
        }

        var result = _app.Profile.UpdateProfile(new UpdateProfileRequest
        {
            UserId = user.Id,
            PhoneNumber = ProfilePhoneBox.Text ?? string.Empty
        });

        SetStatus(result.Message);
        if (result.Succeeded)
        {
            await PersistAsync();
            ApplyRoleUi();
        }
    }

    private async void ChangePassword_Click(object? sender, RoutedEventArgs e)
    {
        var user = _app.CurrentUser.CurrentUser;
        if (user is null)
        {
            return;
        }

        var result = _app.Profile.ChangePassword(new ChangePasswordRequest
        {
            UserId = user.Id,
            CurrentPassword = CurrentPasswordBox.Text ?? string.Empty,
            NewPassword = NewPasswordBox.Text ?? string.Empty
        });

        SetStatus(result.Message);
        if (result.Succeeded)
        {
            await PersistAsync();
        }
    }
}
