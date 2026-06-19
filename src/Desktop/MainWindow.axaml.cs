using System.Threading.Tasks;
using Application;
using Application.DTOs.Auth;
using Application.DTOs.Map;
using Application.DTOs.Profile;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Domain.Enums;

namespace Desktop;

internal sealed record ZoneComboItem(ZoneAvailabilityDto Zone)
{
    public override string ToString() => $"[{Zone.District}] {Zone.Name} ({Zone.AvailableSlots}/{Zone.TotalSlots})";
}

internal sealed record SlotComboItem(string Code, decimal HourlyRate)
{
    public override string ToString() => $"{Code} — {HourlyRate:N0} UZS/soat";
}

internal sealed record VehicleComboItem(Guid Id, string Label)
{
    public override string ToString() => Label;
}

public partial class MainWindow : Window
{
    private readonly ParkingAppServices _app;
    private IReadOnlyList<ZoneAvailabilityDto> _zones = [];
    private Guid? _selectedZoneId;
    private string? _selectedSlotCode;
    private Button? _activeNavButton;

    public MainWindow()
        : this(App.Services)
    {
    }

    public MainWindow(ParkingAppServices app)
    {
        _app = app;
        InitializeComponent();
        SearchLatBox.Text = "41.2995";
        SearchLngBox.Text = "69.2401";
        SearchRadiusBox.ItemsSource = new[] { "1 km", "3 km", "5 km", "10 km" };
        SearchRadiusBox.SelectedIndex = 1;
        _activeNavButton = NavParkingBtn;
        RefreshGuestState();
        RefreshAllZones();
        ShowPage("parking");
    }

    private void RefreshGuestState()
    {
        var hasAdmin = _app.Auth.HasAdmin();
        BootstrapPanel.IsVisible = !hasAdmin;
        RegisterPanel.IsVisible = hasAdmin;
    }

    private async Task PersistAsync() => await _app.StateStore.PersistAsync();

    private void SetStatus(string message) => StatusText.Text = message;

    private void ShowPage(string page)
    {
        ParkingPage.IsVisible = page == "parking";
        AuthPage.IsVisible = page == "auth";
        BookingPage.IsVisible = page == "booking";
        AdminPage.IsVisible = page == "admin";
        OperatorPage.IsVisible = page == "operator";
        ProfilePage.IsVisible = page == "profile";

        SetNavActive(page switch
        {
            "parking" => NavParkingBtn,
            "auth" => NavAuthBtn,
            "booking" => NavBookingBtn,
            "admin" => NavAdminBtn,
            "operator" => NavOperatorBtn,
            "profile" => NavProfileBtn,
            _ => NavParkingBtn
        });
    }

    private void SetNavActive(Button button)
    {
        foreach (var nav in new[] { NavParkingBtn, NavBookingBtn, NavAuthBtn, NavAdminBtn, NavOperatorBtn, NavProfileBtn })
        {
            nav.Classes.Remove("nav-active");
            nav.Classes.Add("nav");
        }

        button.Classes.Remove("nav");
        button.Classes.Add("nav-active");
        _activeNavButton = button;
    }

    private void NavParking_Click(object? sender, RoutedEventArgs e) => ShowPage("parking");
    private void NavAuth_Click(object? sender, RoutedEventArgs e) => ShowPage("auth");
    private void NavBooking_Click(object? sender, RoutedEventArgs e) => ShowPage("booking");
    private void NavAdmin_Click(object? sender, RoutedEventArgs e) => ShowPage("admin");
    private void NavOperator_Click(object? sender, RoutedEventArgs e) => ShowPage("operator");
    private void NavProfile_Click(object? sender, RoutedEventArgs e) => ShowPage("profile");

    private void ReportProblem_Click(object? sender, RoutedEventArgs e) =>
        SetStatus("Muammo xabari qabul qilindi. Tez orada operator bog'lanadi.");

    private void ApplyRoleUi()
    {
        var user = _app.CurrentUser.CurrentUser;
        var isLoggedIn = user is not null;

        NavAuthBtn.IsVisible = !isLoggedIn;
        NavBookingBtn.IsVisible = user?.Role == UserRole.User;
        NavAdminBtn.IsVisible = user?.Role == UserRole.Admin;
        NavOperatorBtn.IsVisible = user?.Role == UserRole.Operator;
        NavProfileBtn.IsVisible = isLoggedIn;
        LogoutButton.IsVisible = isLoggedIn;

        if (isLoggedIn)
        {
            SidebarUserName.Text = user!.Username;
            SidebarUserEmail.Text = $"{user.Role} · {user.PhoneNumber}";
            UserAvatarText.Text = user.Username.Length > 0
                ? char.ToUpperInvariant(user.Username[0]).ToString()
                : "?";

            var profile = _app.Profile.GetProfile(user.Id);
            if (profile.Succeeded && profile.Value is not null)
            {
                ProfileInfoText.Text =
                    $"Username: {profile.Value.Username}\nRol: {profile.Value.Role}\nTelefon: {profile.Value.PhoneNumber}";
            }

            if (user.Role == UserRole.User)
            {
                LoadReservationCombos();
            }

            ShowPage(user.Role switch
            {
                UserRole.Admin => "admin",
                UserRole.Operator => "operator",
                UserRole.User => "parking",
                _ => "profile"
            });
        }
        else
        {
            SidebarUserName.Text = "Mehmon";
            SidebarUserEmail.Text = "Kirish kerak";
            UserAvatarText.Text = "M";
            ShowPage("parking");
        }

        RefreshAllZones();
    }

    private void RefreshAllZones_Click(object? sender, RoutedEventArgs e) => RefreshAllZones();

    private void RefreshAllZones()
    {
        _zones = _app.Map.GetAllZonesWithAvailability();
        BindZones(_zones);
    }

    private void SearchNearby_Click(object? sender, RoutedEventArgs e)
    {
        if (!double.TryParse(SearchLatBox.Text, out var lat) ||
            !double.TryParse(SearchLngBox.Text, out var lng))
        {
            SetStatus("Kenglik va uzunlikni to'g'ri kiriting.");
            return;
        }

        var radius = SearchRadiusBox.SelectedIndex switch
        {
            0 => 1d,
            2 => 5d,
            3 => 10d,
            _ => 3d
        };

        _zones = _app.Map.SearchNearbyZones(new NearbyZoneSearchRequest
        {
            Latitude = lat,
            Longitude = lng,
            RadiusKm = radius
        });

        BindZones(_zones);
        SetStatus(_zones.Count == 0
            ? "Yaqin atrofda parking topilmadi. Radiusni oshiring."
            : $"{_zones.Count} ta yaqin hudud topildi.");
        ShowPage("booking");
    }

    private void BindZones(IReadOnlyList<ZoneAvailabilityDto> zones)
    {
        ZoneListBox.ItemsSource = zones
            .Select(FormatZoneListItem)
            .ToList();
        ZoneListBox.Tag = zones;
        BuildZonePills(zones);
        LoadReservationCombos();

        if (zones.Count == 0)
        {
            ParkingTitleText.Text = "Parking joylar";
            ParkingSubtitleText.Text = "Hudud topilmadi";
            OccupiedStatText.Text = "0";
            EmptyStatText.Text = "0";
            FloorGridPanel.Children.Clear();
            SelectedZoneDetailText.Text = "Ma'lumot yo'q.";
            return;
        }

        var selected = _selectedZoneId.HasValue
            ? zones.FirstOrDefault(z => z.ZoneId == _selectedZoneId.Value)
            : null;
        SelectZone(selected ?? zones[0], navigateToBooking: false);
    }

    private void BuildZonePills(IReadOnlyList<ZoneAvailabilityDto> zones)
    {
        ZonePillsPanel.Children.Clear();
        foreach (var zone in zones)
        {
            var isActive = zone.ZoneId == _selectedZoneId;
            var btn = new Button
            {
                Content = zone.Code,
                Tag = zone
            };
            btn.Classes.Add(isActive ? "zone-pill-active" : "zone-pill");
            btn.Click += ZonePill_Click;
            ZonePillsPanel.Children.Add(btn);
        }
    }

    private void ZonePill_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ZoneAvailabilityDto zone })
        {
            SelectZone(zone, navigateToBooking: false);
        }
    }

    private static string FormatZoneListItem(ZoneAvailabilityDto zone)
    {
        var distance = zone.DistanceKm.HasValue ? $" | {zone.DistanceKm:F1} km" : string.Empty;
        return $"[{zone.District}] {zone.Name} — Bosh: {zone.AvailableSlots}/{zone.TotalSlots}{distance}";
    }

    private void ZoneListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ZoneListBox.Tag is not IReadOnlyList<ZoneAvailabilityDto> zones ||
            ZoneListBox.SelectedIndex < 0 ||
            ZoneListBox.SelectedIndex >= zones.Count)
        {
            return;
        }

        SelectZone(zones[ZoneListBox.SelectedIndex], navigateToBooking: false);
        ShowPage("parking");
    }

    private void SelectZone(ZoneAvailabilityDto zone, bool navigateToBooking)
    {
        _selectedZoneId = zone.ZoneId;
        UpdateZoneDisplay(zone);
        LoadReservationCombos(zone.ZoneId);

        if (navigateToBooking && NavBookingBtn.IsVisible)
        {
            ShowPage("booking");
        }
    }

    private void SlotCell_Click(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control { Tag: ZoneSlotDto slot })
        {
            return;
        }

        _selectedSlotCode = slot.Code;
        if (_selectedZoneId.HasValue)
        {
            var zone = _zones.FirstOrDefault(z => z.ZoneId == _selectedZoneId.Value);
            if (zone is not null)
            {
                var slots = _app.Map.GetZoneSlots(zone.ZoneId, availableOnly: false);
                ParkingFloorRenderer.Render(FloorGridPanel, slots, _selectedSlotCode, SlotCell_Click);
            }
        }

        SetStatus(slot.Status switch
        {
            SlotStatus.Available => $"{slot.Code} — bosh slot. Bron qilish uchun 'Bron' bo'limiga o'ting.",
            SlotStatus.Reserved => $"{slot.Code} — bron qilingan.",
            SlotStatus.Occupied => $"{slot.Code} — band.",
            _ => slot.Code
        });

        if (slot.Status == SlotStatus.Available && NavBookingBtn.IsVisible)
        {
            LoadReservationCombos(_selectedZoneId);
            var zoneItems = ReservationZoneBox.ItemsSource as List<ZoneComboItem>;
            var slotItems = ReservationSlotBox.ItemsSource as List<SlotComboItem>;
            if (zoneItems is not null && _selectedZoneId.HasValue)
            {
                var zi = zoneItems.FindIndex(z => z.Zone.ZoneId == _selectedZoneId.Value);
                if (zi >= 0)
                {
                    ReservationZoneBox.SelectedIndex = zi;
                }
            }

            if (slotItems is not null)
            {
                var si = slotItems.FindIndex(s => string.Equals(s.Code, slot.Code, StringComparison.OrdinalIgnoreCase));
                if (si >= 0)
                {
                    ReservationSlotBox.SelectedIndex = si;
                }
            }

            ShowPage("booking");
        }
    }

    private void LoadReservationCombos(Guid? preferredZoneId = null)
    {
        var zones = _app.Map.GetAllZonesWithAvailability();
        var zoneItems = zones.Select(z => new ZoneComboItem(z)).ToList();
        ReservationZoneBox.ItemsSource = zoneItems;

        if (zones.Count == 0)
        {
            return;
        }

        var zoneToSelect = preferredZoneId ?? _selectedZoneId ?? zones[0].ZoneId;
        var index = zoneItems.FindIndex(z => z.Zone.ZoneId == zoneToSelect);
        ReservationZoneBox.SelectedIndex = index >= 0 ? index : 0;
        ReservationZoneBox.SelectionChanged -= ReservationZoneBox_SelectionChanged;
        ReservationZoneBox.SelectionChanged += ReservationZoneBox_SelectionChanged;

        var user = _app.CurrentUser.CurrentUser;
        if (user?.Role == UserRole.User)
        {
            ReservationVehicleBox.ItemsSource = _app.User.GetUserVehicles(user.Id)
                .Select(v => new VehicleComboItem(v.Id, $"{v.PlateNumber} ({v.Model})"))
                .ToList();
            if (ReservationVehicleBox.ItemCount > 0)
            {
                ReservationVehicleBox.SelectedIndex = 0;
            }
        }

        OnReservationZoneChanged();
    }

    private void ReservationZoneBox_SelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        OnReservationZoneChanged();

    private void OnReservationZoneChanged()
    {
        if (ReservationZoneBox.SelectedItem is not ZoneComboItem zoneItem)
        {
            return;
        }

        _selectedZoneId = zoneItem.Zone.ZoneId;
        var zone = _zones.FirstOrDefault(z => z.ZoneId == zoneItem.Zone.ZoneId) ?? zoneItem.Zone;
        UpdateZoneDisplay(zone);

        var slots = _app.Map.GetZoneSlots(zoneItem.Zone.ZoneId, availableOnly: true);
        ReservationSlotBox.ItemsSource = slots.Select(s => new SlotComboItem(s.Code, s.HourlyRate)).ToList();
        if (ReservationSlotBox.ItemCount > 0)
        {
            ReservationSlotBox.SelectedIndex = 0;
        }
    }

    private void UpdateZoneDisplay(ZoneAvailabilityDto zone)
    {
        ParkingTitleText.Text = zone.Name;
        ParkingSubtitleText.Text = $"{zone.District} · {zone.Address}";
        OccupiedStatText.Text = zone.OccupiedSlots.ToString();
        EmptyStatText.Text = zone.AvailableSlots.ToString();

        SelectedZoneDetailText.Text =
            $"Jami: {zone.TotalSlots} slot | Band: {zone.OccupiedSlots} | Bron: {zone.ReservedSlots} | Bosh: {zone.AvailableSlots}\n" +
            $"Tarif: {zone.HourlyRate:N0} UZS/soat" +
            (zone.DistanceKm.HasValue ? $" | Masofa: {zone.DistanceKm:F1} km" : string.Empty);

        var slots = _app.Map.GetZoneSlots(zone.ZoneId, availableOnly: false);
        ParkingFloorRenderer.Render(FloorGridPanel, slots, _selectedSlotCode, SlotCell_Click);
        BuildZonePills(_zones);
    }

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

        _app.CurrentUser.SetCurrentUser(_app.Query.FindUser(result.Value.UserId));
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
        _selectedSlotCode = null;
        ApplyRoleUi();
        RefreshGuestState();
        SetStatus("Chiqildi.");
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
        var zones = _app.Map.GetAllZonesWithAvailability();
        OverviewText.Text =
            $"Hududlar: {zones.Count} | Slotlar: {_app.Query.GetSlots().Count} | " +
            $"Bosh slotlar: {zones.Sum(z => z.AvailableSlots)} | " +
            $"Bronlar: {_app.Query.GetAllReservations().Count}";
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
            _app.CurrentUser.CurrentUser.Id, userId, vehicleId, CheckInSlotBox.Text ?? string.Empty);

        SetStatus(result.Message);
        if (result.Succeeded)
        {
            await PersistAsync();
            RefreshAllZones();
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
            RefreshAllZones();
        }
    }

    private void RefreshSessions_Click(object? sender, RoutedEventArgs e)
    {
        SessionsListBox.ItemsSource = _app.Query.GetActiveSessions()
            .Select(s => $"Session {s.Id} | User {s.UserId} | Slot {s.SlotId}")
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

        var result = _app.User.AddVehicle(user.Id, PlateBox.Text ?? string.Empty,
            ModelBox.Text ?? string.Empty, ColorBox.Text ?? string.Empty);

        SetStatus(result.Message);
        if (result.Succeeded)
        {
            await PersistAsync();
            LoadReservationCombos(_selectedZoneId);
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

        if (ReservationZoneBox.SelectedItem is not ZoneComboItem zoneItem ||
            ReservationSlotBox.SelectedItem is not SlotComboItem slotItem)
        {
            SetStatus("Hudud va slot tanlang.");
            return;
        }

        if (ReservationVehicleBox.SelectedItem is not VehicleComboItem vehicleItem)
        {
            SetStatus("Avval avtomobil qo'shing.");
            return;
        }

        var from = DateTime.UtcNow.AddHours(1);
        var to = from.AddHours(2);

        var result = _app.User.CreateReservation(
            user.Id,
            vehicleItem.Id,
            zoneItem.Zone.ZoneId,
            slotItem.Code,
            from,
            to);
        SetStatus(result.Message);
        if (result.Succeeded)
        {
            await PersistAsync();
            RefreshAllZones();
            LoadReservationCombos(zoneItem.Zone.ZoneId);
            ShowPage("parking");
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
            items.Add($"Avtomobil: {v.PlateNumber} ({v.Model}) — ID: {v.Id}");
        }

        foreach (var r in _app.User.GetUserReservations(user.Id))
        {
            var slot = _app.Query.FindSlot(r.SlotId);
            items.Add($"Bron: {slot?.Code ?? r.SlotId.ToString()} — {r.Status}");
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
