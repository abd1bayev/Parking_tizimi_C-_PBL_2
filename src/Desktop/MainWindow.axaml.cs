using System.Threading.Tasks;
using Application;
using Application.DTOs.Auth;
using Application.DTOs.Map;
using Application.DTOs.Profile;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Application.DTOs.Problems;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Domain.Entities;
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

internal sealed record UserComboItem(Guid Id, string Label)
{
    public override string ToString() => Label;
}

internal sealed record SessionComboItem(Guid Id, string Label)
{
    public override string ToString() => Label;
}

internal sealed record ReservationListItem(Guid Id, string Label)
{
    public override string ToString() => Label;
}

internal sealed record ProblemListItem(Guid Id, string Label)
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
        DashboardMapCanvas.SizeChanged += (_, _) => RenderDashboardMap();
        _activeNavButton = NavDashboardBtn;
        RefreshGuestState();
        RefreshAllZones();
        RefreshDashboard();
        ShowPage(GetDefaultPageForCurrentUser(), skipGuard: true);
    }

    private void RefreshGuestState()
    {
        ApplyRoleNavigation();
        if (_app.CurrentUser.CurrentUser is null && !_app.Auth.HasAdmin())
        {
            ShowPage("setup", skipGuard: true);
        }
    }

    private void ApplyRoleNavigation()
    {
        var user = _app.CurrentUser.CurrentUser;
        var isLoggedIn = user is not null;
        var hasAdmin = _app.Auth.HasAdmin();
        var role = user?.Role;

        var showDashboard = true;
        var showParking = true;
        var showBooking = role == UserRole.User;
        var showTariffs = true;
        var showPayments = role is UserRole.User or UserRole.Admin;
        var showReports = role == UserRole.Admin;
        var showSessions = role is UserRole.Operator or UserRole.Admin;
        var showProblems = true;
        var showUsers = role == UserRole.Admin;
        var showOperator = role == UserRole.Operator;
        var showAdmin = role == UserRole.Admin;
        var showProfile = isLoggedIn;
        var showSetup = !hasAdmin && !isLoggedIn;
        var showLogin = !isLoggedIn;
        var showRegister = hasAdmin && !isLoggedIn;

        NavDashboardBtn.IsVisible = showDashboard;
        NavParkingBtn.IsVisible = showParking;
        NavBookingBtn.IsVisible = showBooking;
        NavTariffsBtn.IsVisible = showTariffs;
        NavPaymentsBtn.IsVisible = showPayments;
        NavReportsBtn.IsVisible = showReports;
        NavSessionsBtn.IsVisible = showSessions;
        NavProblemsBtn.IsVisible = showProblems;
        NavUsersBtn.IsVisible = showUsers;
        NavOperatorBtn.IsVisible = showOperator;
        NavAdminBtn.IsVisible = showAdmin;
        NavProfileBtn.IsVisible = showProfile;
        NavSetupBtn.IsVisible = showSetup;
        NavLoginBtn.IsVisible = showLogin;
        NavRegisterBtn.IsVisible = showRegister;

        SectionAsosiyLabel.IsVisible = showDashboard || showParking || showBooking || showTariffs;
        SectionMoliyaLabel.IsVisible = showPayments || showReports;
        SectionOperatsiyaLabel.IsVisible = showSessions || showProblems;
        SectionBoshqaruvLabel.IsVisible = showUsers || showOperator || showAdmin;
        SectionHisobLabel.IsVisible = showProfile || showSetup || showLogin || showRegister;

        ResolveProblemPageBtn.IsVisible = role == UserRole.Admin;
        GoToRegisterLink.IsVisible = showRegister;
        LogoutButton.IsVisible = isLoggedIn;

        SidebarRoleSubtitle.Text = role switch
        {
            UserRole.Admin => "Ma'mur paneli",
            UserRole.Operator => "Operator paneli",
            UserRole.User => "Foydalanuvchi paneli",
            _ => hasAdmin ? "Mehmon — ko'rish rejimi" : "Birinchi sozlash"
        };
    }

    private static bool CanAccessPage(User? user, bool hasAdmin, string page) =>
        page switch
        {
            "dashboard" or "parking" or "tariffs" or "problems" => true,
            "setup" => !hasAdmin && user is null,
            "login" => user is null,
            "register" => user is null && hasAdmin,
            "booking" => user?.Role == UserRole.User,
            "payments" => user?.Role is UserRole.User or UserRole.Admin,
            "reports" or "users" or "admin" => user?.Role == UserRole.Admin,
            "operator" => user?.Role == UserRole.Operator,
            "sessions" => user?.Role is UserRole.Operator or UserRole.Admin,
            "profile" => user is not null,
            _ => false
        };

    private string GetDefaultPageForCurrentUser()
    {
        var user = _app.CurrentUser.CurrentUser;
        if (user is null)
        {
            return _app.Auth.HasAdmin() ? "login" : "setup";
        }

        return user.Role switch
        {
            UserRole.Admin => "dashboard",
            UserRole.Operator => "operator",
            UserRole.User => "parking",
            _ => "dashboard"
        };
    }

    private async Task PersistAsync() => await _app.StateStore.PersistAsync();

    private void SetStatus(string message) => StatusText.Text = message;

    private void ShowPage(string page, bool skipGuard = false)
    {
        var user = _app.CurrentUser.CurrentUser;
        var hasAdmin = _app.Auth.HasAdmin();

        if (!skipGuard && !CanAccessPage(user, hasAdmin, page))
        {
            SetStatus("Bu bo'lim sizning rolingiz uchun ochiq emas.");
            var fallback = GetDefaultPageForCurrentUser();
            if (!string.Equals(fallback, page, StringComparison.Ordinal))
            {
                ShowPage(fallback, skipGuard: true);
            }

            return;
        }

        DashboardPage.IsVisible = page == "dashboard";
        ParkingPage.IsVisible = page == "parking";
        SetupPage.IsVisible = page == "setup";
        LoginPage.IsVisible = page == "login";
        RegisterPage.IsVisible = page == "register";
        BookingPage.IsVisible = page == "booking";
        AdminPage.IsVisible = page == "admin";
        OperatorPage.IsVisible = page == "operator";
        ProfilePage.IsVisible = page == "profile";
        TariffsPage.IsVisible = page == "tariffs";
        PaymentsPage.IsVisible = page == "payments";
        ReportsPage.IsVisible = page == "reports";
        SessionsPage.IsVisible = page == "sessions";
        ProblemsPage.IsVisible = page == "problems";
        UsersPage.IsVisible = page == "users";

        switch (page)
        {
            case "dashboard":
                RefreshDashboard();
                break;
            case "operator":
                LoadOperatorCombos();
                break;
            case "tariffs":
                RefreshTariffs();
                break;
            case "payments":
                RefreshPaymentsPage();
                break;
            case "reports":
                RefreshReportsPage();
                break;
            case "sessions":
                RefreshAllSessionsPage();
                break;
            case "problems":
                RefreshProblemsPage();
                break;
            case "users":
                RefreshUsersPage();
                break;
            case "booking":
                if (_app.CurrentUser.CurrentUser?.Role == UserRole.User)
                {
                    LoadReservationCombos();
                    RefreshUserData_Click(null, null!);
                }
                break;
        }

        SetNavActive(page switch
        {
            "dashboard" => NavDashboardBtn,
            "parking" => NavParkingBtn,
            "setup" => NavSetupBtn,
            "login" => NavLoginBtn,
            "register" => NavRegisterBtn,
            "booking" => NavBookingBtn,
            "tariffs" => NavTariffsBtn,
            "payments" => NavPaymentsBtn,
            "reports" => NavReportsBtn,
            "sessions" => NavSessionsBtn,
            "problems" => NavProblemsBtn,
            "users" => NavUsersBtn,
            "admin" => NavAdminBtn,
            "operator" => NavOperatorBtn,
            "profile" => NavProfileBtn,
            _ => NavDashboardBtn
        });
    }

    private static Button[] AllNavButtons(MainWindow w) =>
    [
        w.NavDashboardBtn, w.NavParkingBtn, w.NavBookingBtn, w.NavTariffsBtn,
        w.NavPaymentsBtn, w.NavReportsBtn, w.NavSessionsBtn, w.NavProblemsBtn,
        w.NavUsersBtn, w.NavOperatorBtn, w.NavAdminBtn, w.NavProfileBtn,
        w.NavSetupBtn, w.NavLoginBtn, w.NavRegisterBtn
    ];

    private void SetNavActive(Button button)
    {
        foreach (var nav in AllNavButtons(this))
        {
            nav.Classes.Remove("nav-active");
            nav.Classes.Add("nav");
        }

        button.Classes.Remove("nav");
        button.Classes.Add("nav-active");
        _activeNavButton = button;
    }

    private void NavDashboard_Click(object? sender, RoutedEventArgs e) => ShowPage("dashboard");
    private void NavParking_Click(object? sender, RoutedEventArgs e) => ShowPage("parking");
    private void NavSetup_Click(object? sender, RoutedEventArgs e) => ShowPage("setup");
    private void NavLogin_Click(object? sender, RoutedEventArgs e) => ShowPage("login");
    private void NavRegister_Click(object? sender, RoutedEventArgs e) => ShowPage("register");
    private void GoToRegister_Click(object? sender, RoutedEventArgs e) => ShowPage("register");
    private void GoToLogin_Click(object? sender, RoutedEventArgs e) => ShowPage("login");
    private void NavBooking_Click(object? sender, RoutedEventArgs e) => ShowPage("booking");
    private void NavAdmin_Click(object? sender, RoutedEventArgs e) => ShowPage("admin");
    private void NavOperator_Click(object? sender, RoutedEventArgs e) => ShowPage("operator");
    private void NavProfile_Click(object? sender, RoutedEventArgs e) => ShowPage("profile");
    private void NavTariffs_Click(object? sender, RoutedEventArgs e) => ShowPage("tariffs");
    private void NavPayments_Click(object? sender, RoutedEventArgs e) => ShowPage("payments");
    private void NavReports_Click(object? sender, RoutedEventArgs e) => ShowPage("reports");
    private void NavSessions_Click(object? sender, RoutedEventArgs e) => ShowPage("sessions");
    private void NavProblems_Click(object? sender, RoutedEventArgs e) => ShowPage("problems");
    private void NavUsers_Click(object? sender, RoutedEventArgs e) => ShowPage("users");
    private void RefreshReports_Click(object? sender, RoutedEventArgs e) => RefreshReportsPage();
    private void RefreshAllSessions_Click(object? sender, RoutedEventArgs e) => RefreshAllSessionsPage();
    private void RefreshProblemsPage_Click(object? sender, RoutedEventArgs e) => RefreshProblemsPage();
    private void RefreshUsersPage_Click(object? sender, RoutedEventArgs e) => RefreshUsersPage();

    private async void ReportProblem_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new ProblemReportWindow();
        var result = await dialog.ShowDialog<bool>(this);
        if (!result)
        {
            return;
        }

        var reportResult = _app.Problems.Report(new ReportProblemRequest
        {
            ReporterUserId = _app.CurrentUser.CurrentUser?.Id,
            ZoneId = _selectedZoneId,
            SlotCode = _selectedSlotCode,
            Title = dialog.ReportTitle,
            Description = dialog.ReportDescription
        });

        SetStatus(reportResult.Message);
        if (reportResult.Succeeded)
        {
            await PersistAsync();
            RefreshDashboard();
            RefreshProblemsPage();
        }
    }

    private void RefreshDashboard()
    {
        var user = _app.CurrentUser.CurrentUser;
        var role = user?.Role;
        var overview = _app.Dashboard.GetOverview();

        DashOccupiedText.Text = overview.OccupiedSlots.ToString();
        DashOccupancyPercentText.Text = $"{overview.OccupancyPercent}% bandlik";

        ZoneStatsPanel.Items.Clear();
        foreach (var zone in overview.ZoneStats)
        {
            ZoneStatsPanel.Items.Add(CreateZoneStatRow(zone.Name, zone.Code, zone.OccupancyPercent, zone.Occupied, zone.Total));
        }

        RenderDashboardMap();

        DashRevenueChartSection.IsVisible = role == UserRole.Admin;
        DashUserActionsSection.IsVisible = role is null or UserRole.User;
        DashFourthValueText.Foreground = role == UserRole.Admin
            ? new SolidColorBrush(Color.Parse("#DC2626"))
            : new SolidColorBrush(Color.Parse("#0F172A"));

        if (role == UserRole.Admin)
        {
            ApplyAdminDashboard(overview);
        }
        else if (role == UserRole.Operator)
        {
            ApplyOperatorDashboard(overview);
        }
        else if (role == UserRole.User)
        {
            ApplyUserDashboard(overview, user!);
        }
        else
        {
            ApplyGuestDashboard(overview);
        }

        if (role == UserRole.Admin)
        {
            RevenueChartPanel.Items.Clear();
            var maxRevenue = overview.Last7DaysRevenue.Max(r => r.Amount);
            if (maxRevenue <= 0)
            {
                maxRevenue = 1;
            }

            foreach (var day in overview.Last7DaysRevenue)
            {
                RevenueChartPanel.Items.Add(CreateRevenueBar(day.Date, day.Amount, maxRevenue));
            }
        }
    }

    private void ApplyAdminDashboard(Application.DTOs.Dashboard.DashboardOverviewDto overview)
    {
        DashboardTitleText.Text = "Ma'mur boshqaruv paneli";
        DashboardSubtitleText.Text = "Tizim bo'yicha to'liq statistika va nazorat";

        DashCard1Label.Text = "Band joylar";
        DashCard2Label.Text = "Bugungi daromad";
        DashCard3Label.Text = "Faol sessiyalar";
        DashCard4Label.Text = "Ochiq muammolar";

        DashSecondValueText.Text = $"{overview.TodayRevenue:N0} UZS";
        DashSecondHintText.Text = $"Jami: {overview.TotalRevenue:N0} UZS";
        DashThirdValueText.Text = overview.ActiveSessions.ToString();
        DashThirdHintText.Text = $"Bron: {overview.ActiveReservations}";
        DashFourthValueText.Text = overview.OpenProblems.ToString();
        DashFourthHintText.Text = $"Foydalanuvchilar: {overview.TotalUsers}";

        SetDashboardCardsVisible(showSecond: true, showThird: true, showFourth: true);
    }

    private void ApplyOperatorDashboard(Application.DTOs.Dashboard.DashboardOverviewDto overview)
    {
        DashboardTitleText.Text = "Operator boshqaruv paneli";
        DashboardSubtitleText.Text = "Kirish/chiqish va faol park sessiyalari";

        DashCard1Label.Text = "Band joylar";
        DashCard2Label.Text = "Faol sessiyalar";
        DashCard3Label.Text = "Faol bronlar";
        DashCard4Label.Text = "Ochiq muammolar";

        DashSecondValueText.Text = overview.ActiveSessions.ToString();
        DashSecondHintText.Text = "Hozir parkingda";
        DashThirdValueText.Text = overview.ActiveReservations.ToString();
        DashThirdHintText.Text = "Kutilayotgan bronlar";
        DashFourthValueText.Text = overview.OpenProblems.ToString();
        DashFourthHintText.Text = "Tekshirish kerak";

        SetDashboardCardsVisible(showSecond: true, showThird: true, showFourth: true);
    }

    private void ApplyUserDashboard(Application.DTOs.Dashboard.DashboardOverviewDto overview, User user)
    {
        DashboardTitleText.Text = "Mening panelim";
        DashboardSubtitleText.Text = "Bronlar, to'lovlar va bo'sh park joylari";

        var payments = _app.User.GetUserPayments(user.Id);
        var reservations = _app.User.GetUserReservations(user.Id)
            .Count(r => r.Status == Domain.Enums.ReservationStatus.Active);

        DashCard1Label.Text = "Bo'sh joylar";
        DashCard2Label.Text = "Mening to'lovlarim";
        DashCard3Label.Text = "Faol bronlarim";
        DashCard4Label.Text = "Band joylar";

        DashOccupiedText.Text = overview.AvailableSlots.ToString();
        DashOccupancyPercentText.Text = $"{overview.TotalZones} ta hudud";
        DashSecondValueText.Text = $"{payments.Sum(p => p.Amount):N0} UZS";
        DashSecondHintText.Text = $"{payments.Count} ta to'lov";
        DashThirdValueText.Text = reservations.ToString();
        DashThirdHintText.Text = "Faol bronlar";
        DashFourthValueText.Text = overview.OccupiedSlots.ToString();
        DashFourthHintText.Text = $"{overview.OccupancyPercent}% bandlik";

        DashUserActionsTitle.Text = "Tez harakatlar";
        DashUserActionsText.Text = "Park joy tanlang, bron qiling yoki avtomobilingizni qo'shing.";
        DashActionPrimaryBtn.Content = "Bron qilish";
        DashActionSecondaryBtn.Content = "Park xaritasi";
        DashActionSecondaryBtn.IsVisible = true;

        SetDashboardCardsVisible(showSecond: true, showThird: true, showFourth: true);
    }

    private void ApplyGuestDashboard(Application.DTOs.Dashboard.DashboardOverviewDto overview)
    {
        DashboardTitleText.Text = "Park ko'rinishi";
        DashboardSubtitleText.Text = "Umumiy bandlik — bron qilish uchun tizimga kiring";

        DashCard1Label.Text = "Bo'sh joylar";
        DashCard2Label.Text = "Jami joylar";
        DashCard3Label.Text = "Park hududlari";

        DashOccupiedText.Text = overview.AvailableSlots.ToString();
        DashOccupancyPercentText.Text = $"{overview.OccupancyPercent}% band";
        DashSecondValueText.Text = overview.TotalSlots.ToString();
        DashSecondHintText.Text = $"Band: {overview.OccupiedSlots}";
        DashThirdValueText.Text = overview.TotalZones.ToString();
        DashThirdHintText.Text = "Toshkent bo'ylab";

        DashUserActionsTitle.Text = "Tizimga kiring";
        DashUserActionsText.Text = "Bron qilish va to'lovlarni ko'rish uchun kiring yoki ro'yxatdan o'ting.";
        DashActionPrimaryBtn.Content = "Kirish";
        DashActionSecondaryBtn.Content = "Ro'yxatdan o'tish";
        DashActionSecondaryBtn.IsVisible = _app.Auth.HasAdmin();

        SetDashboardCardsVisible(showSecond: true, showThird: true, showFourth: false);
    }

    private void SetDashboardCardsVisible(bool showSecond, bool showThird, bool showFourth)
    {
        DashCardOccupied.IsVisible = true;
        DashCardSecond.IsVisible = showSecond;
        DashCardThird.IsVisible = showThird;
        DashCardFourth.IsVisible = showFourth;
        DashMapSection.IsVisible = true;
        DashZoneStatsSection.IsVisible = true;
    }

    private void DashActionPrimary_Click(object? sender, RoutedEventArgs e)
    {
        var role = _app.CurrentUser.CurrentUser?.Role;
        if (role == UserRole.User)
        {
            ShowPage("booking");
            return;
        }

        ShowPage("login");
    }

    private void DashActionSecondary_Click(object? sender, RoutedEventArgs e)
    {
        var user = _app.CurrentUser.CurrentUser;
        if (user?.Role == UserRole.User)
        {
            ShowPage("parking");
            return;
        }

        if (user is null && _app.Auth.HasAdmin())
        {
            ShowPage("register");
            return;
        }

        ShowPage("parking");
    }

    private static StackPanel CreateZoneStatRow(string name, string code, double percent, int occupied, int total)
    {
        var barWidth = Math.Clamp(percent / 100d * 220, 4, 220);
        return new StackPanel
        {
            Spacing = 6,
            Margin = new Thickness(0, 0, 0, 12),
            Children =
            {
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                    Children =
                    {
                        new TextBlock { Text = $"{name} ({code})", FontWeight = FontWeight.SemiBold },
                        new TextBlock { Text = $"{occupied}/{total} ({percent}%)", Foreground = new SolidColorBrush(Color.Parse("#64748B")), HorizontalAlignment = HorizontalAlignment.Right }
                    }
                },
                new Border
                {
                    Height = 8,
                    Width = barWidth,
                    CornerRadius = new CornerRadius(4),
                    Background = new SolidColorBrush(Color.Parse("#6366F1")),
                    HorizontalAlignment = HorizontalAlignment.Left
                }
            }
        };
    }

    private static Grid CreateRevenueBar(DateOnly date, decimal amount, decimal maxAmount)
    {
        var height = Math.Max(8, (double)(amount / maxAmount) * 80);
        var grid = new Grid
        {
            Width = 72,
            Margin = new Thickness(0, 0, 12, 0),
            RowDefinitions = new RowDefinitions("*,Auto,Auto")
        };

        var barHost = new Border { VerticalAlignment = VerticalAlignment.Bottom, Height = 90, Background = new SolidColorBrush(Color.Parse("#F1F5F9")), CornerRadius = new CornerRadius(6) };
        var bar = new Border
        {
            Height = height,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = new SolidColorBrush(Color.Parse("#6366F1")),
            CornerRadius = new CornerRadius(6, 6, 0, 0)
        };
        barHost.Child = bar;
        Grid.SetRow(barHost, 0);
        grid.Children.Add(barHost);

        var amountText = new TextBlock
        {
            Text = amount > 0 ? $"{amount / 1000:0}k" : "0",
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = new SolidColorBrush(Color.Parse("#64748B"))
        };
        Grid.SetRow(amountText, 1);
        grid.Children.Add(amountText);

        var dateText = new TextBlock
        {
            Text = date.ToString("dd.MM"),
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = new SolidColorBrush(Color.Parse("#94A3B8"))
        };
        Grid.SetRow(dateText, 2);
        grid.Children.Add(dateText);
        return grid;
    }

    private void RenderDashboardMap() => ZoneMapRenderer.Render(DashboardMapCanvas, _zones, _selectedZoneId);

    private void DashboardMapCanvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var zoneId = ZoneMapRenderer.HitTest(DashboardMapCanvas, e.GetPosition(DashboardMapCanvas));
        if (zoneId is null)
        {
            return;
        }

        var zone = _zones.FirstOrDefault(z => z.ZoneId == zoneId);
        if (zone is not null)
        {
            SelectZone(zone, navigateToBooking: false);
            ShowPage("parking");
        }
    }

    private void ApplyRoleUi()
    {
        var user = _app.CurrentUser.CurrentUser;
        var isLoggedIn = user is not null;

        ApplyRoleNavigation();

        if (isLoggedIn)
        {
            SidebarUserName.Text = user!.Username;
            SidebarUserEmail.Text = $"{UiLabels.Role(user.Role)} · {user.PhoneNumber}";
            UserAvatarText.Text = user.Username.Length > 0
                ? char.ToUpperInvariant(user.Username[0]).ToString()
                : "?";

            var profile = _app.Profile.GetProfile(user.Id);
            if (profile.Succeeded && profile.Value is not null)
            {
                ProfileInfoText.Text =
                    $"Foydalanuvchi nomi: {profile.Value.Username}\nRol: {UiLabels.Role(profile.Value.Role)}\nTelefon: {profile.Value.PhoneNumber}";
            }

            if (user.Role == UserRole.User)
            {
                LoadReservationCombos();
            }

            ShowPage(GetDefaultPageForCurrentUser(), skipGuard: true);
        }
        else
        {
            SidebarUserName.Text = "Mehmon";
            SidebarUserEmail.Text = "Kirish kerak";
            UserAvatarText.Text = "M";
            ShowPage(GetDefaultPageForCurrentUser(), skipGuard: true);
        }

        RefreshDashboard();
        RefreshAllZones();
    }

    private void RefreshAllZones_Click(object? sender, RoutedEventArgs e) => RefreshAllZones();

    private void RefreshAllZones()
    {
        _zones = _app.Map.GetAllZonesWithAvailability();
        BindZones(_zones);
        RefreshDashboard();
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
            ? "Yaqin atrofda park joyi topilmadi. Radiusni oshiring."
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
            ParkingTitleText.Text = "Park joylari";
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
        return $"[{zone.District}] {zone.Name} — Bo'sh: {zone.AvailableSlots}/{zone.TotalSlots}{distance}";
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
            SlotStatus.Available => $"{slot.Code} — bo'sh joy. Bron qilish uchun «Bron» bo'limiga o'ting.",
            SlotStatus.Reserved => $"{slot.Code} — bron qilingan.",
            SlotStatus.Occupied => $"{slot.Code} — band.",
            _ => slot.Code
        });

        if (slot.Status == SlotStatus.Available && _app.CurrentUser.CurrentUser?.Role == UserRole.User)
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
            $"Jami: {zone.TotalSlots} joy | Band: {zone.OccupiedSlots} | Bron: {zone.ReservedSlots} | Bo'sh: {zone.AvailableSlots}\n" +
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
            ShowPage("login");
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
            ShowPage("login");
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
            SetStatus("Faqat ma'mur operator yarata oladi.");
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

    private void RefreshUsers_Click(object? sender, RoutedEventArgs e) => RefreshUsersPage();

    private void RefreshOverview_Click(object? sender, RoutedEventArgs e)
    {
        var zones = _app.Map.GetAllZonesWithAvailability();
        OverviewText.Text =
            $"Hududlar: {zones.Count} | Joylar: {_app.Query.GetSlots().Count} | " +
            $"Bo'sh joylar: {zones.Sum(z => z.AvailableSlots)} | " +
            $"Bronlar: {_app.Query.GetAllReservations().Count}";
    }

    private async void CheckIn_Click(object? sender, RoutedEventArgs e)
    {
        if (_app.CurrentUser.CurrentUser?.Role != UserRole.Operator)
        {
            SetStatus("Faqat operator kirish qilishi mumkin.");
            return;
        }

        if (CheckInUserBox.SelectedItem is not UserComboItem userItem ||
            CheckInVehicleBox.SelectedItem is not VehicleComboItem vehicleItem ||
            CheckInSlotBox.SelectedItem is not SlotComboItem slotItem)
        {
            SetStatus("Foydalanuvchi, avtomobil va park joyini tanlang.");
            return;
        }

        var result = _app.Operator.CheckIn(
            _app.CurrentUser.CurrentUser.Id, userItem.Id, vehicleItem.Id, slotItem.Code);

        SetStatus(result.Message);
        if (result.Succeeded)
        {
            await PersistAsync();
            RefreshAllZones();
            LoadOperatorCombos();
            RefreshDashboard();
        }
    }

    private async void CheckOut_Click(object? sender, RoutedEventArgs e)
    {
        if (_app.CurrentUser.CurrentUser?.Role != UserRole.Operator)
        {
            SetStatus("Faqat operator chiqish qilishi mumkin.");
            return;
        }

        if (CheckOutSessionBox.SelectedItem is not SessionComboItem sessionItem)
        {
            SetStatus("Sessiya tanlang.");
            return;
        }

        var result = _app.Operator.CheckOut(_app.CurrentUser.CurrentUser.Id, sessionItem.Id);
        SetStatus(result.Message);
        if (result.Succeeded)
        {
            await PersistAsync();
            RefreshAllZones();
            LoadOperatorCombos();
            RefreshDashboard();
        }
    }

    private void LoadOperatorCombos()
    {
        var users = _app.Query.GetAllUsers()
            .Where(user => user.Role == UserRole.User && user.IsActive)
            .Select(user => new UserComboItem(user.Id, $"{user.Username} ({user.PhoneNumber})"))
            .ToList();
        CheckInUserBox.ItemsSource = users;
        if (users.Count > 0)
        {
            CheckInUserBox.SelectedIndex = 0;
        }

        CheckInUserBox.SelectionChanged -= CheckInUserBox_SelectionChanged;
        CheckInUserBox.SelectionChanged += CheckInUserBox_SelectionChanged;
        OnCheckInUserChanged();

        var slots = _app.Query.GetSlots()
            .Where(slot => slot.Status is SlotStatus.Available or SlotStatus.Reserved)
            .OrderBy(slot => slot.Code)
            .Select(slot => new SlotComboItem(slot.Code, slot.HourlyRate))
            .ToList();
        CheckInSlotBox.ItemsSource = slots;
        if (slots.Count > 0)
        {
            CheckInSlotBox.SelectedIndex = 0;
        }

        RefreshOperatorSessions();
    }

    private void CheckInUserBox_SelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        OnCheckInUserChanged();

    private void OnCheckInUserChanged()
    {
        if (CheckInUserBox.SelectedItem is not UserComboItem userItem)
        {
            return;
        }

        var vehicles = _app.User.GetUserVehicles(userItem.Id)
            .Select(vehicle => new VehicleComboItem(vehicle.Id, $"{vehicle.PlateNumber} ({vehicle.Model})"))
            .ToList();
        CheckInVehicleBox.ItemsSource = vehicles;
        if (vehicles.Count > 0)
        {
            CheckInVehicleBox.SelectedIndex = 0;
        }
    }

    private void RefreshOperatorSessions()
    {
        var sessions = _app.Query.GetActiveSessions()
            .Select(session =>
            {
                var user = _app.Query.FindUser(session.UserId);
                var slot = _app.Query.FindSlot(session.SlotId);
                return new SessionComboItem(session.Id,
                    $"{user?.Username ?? "?"} | {slot?.Code ?? "?"} | {session.CheckInUtc:HH:mm}");
            })
            .ToList();

        CheckOutSessionBox.ItemsSource = sessions;
        SessionsListBox.ItemsSource = sessions.Select(s => s.Label).ToList();
        if (sessions.Count > 0)
        {
            CheckOutSessionBox.SelectedIndex = 0;
        }
    }

    private void RefreshSessions_Click(object? sender, RoutedEventArgs e) => RefreshOperatorSessions();

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
            SetStatus("Hudud va park joyini tanlang.");
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
            items.Add($"Avtomobil: {v.PlateNumber} ({v.Model})");
        }

        UserDataListBox.ItemsSource = items;

        ReservationsListBox.ItemsSource = _app.User.GetUserReservations(user.Id)
            .Select(reservation =>
            {
                var slot = _app.Query.FindSlot(reservation.SlotId);
                return new ReservationListItem(reservation.Id,
                    $"{slot?.Code ?? "?"} | {UiLabels.FormatReservationStatus(reservation.Status)} | {reservation.ReservedFromUtc:dd.MM HH:mm}");
            })
            .ToList();

        PaymentsListBox.ItemsSource = _app.User.GetUserPayments(user.Id)
            .Select(payment => $"{payment.Amount:N0} UZS | {UiLabels.FormatPaymentStatus(payment.Status)} | {payment.PaidAtUtc:dd.MM.yyyy}")
            .ToList();
    }

    private async void CancelReservation_Click(object? sender, RoutedEventArgs e)
    {
        var user = _app.CurrentUser.CurrentUser;
        if (user?.Role != UserRole.User)
        {
            SetStatus("Faqat foydalanuvchi bronni bekor qila oladi.");
            return;
        }

        if (ReservationsListBox.SelectedItem is not ReservationListItem reservationItem)
        {
            SetStatus("Bekor qilinadigan bronni tanlang.");
            return;
        }

        var result = _app.User.CancelReservation(user.Id, reservationItem.Id);
        SetStatus(result.Message);
        if (result.Succeeded)
        {
            await PersistAsync();
            RefreshAllZones();
            RefreshUserData_Click(null, null!);
            RefreshDashboard();
        }
    }

    private void RefreshTariffs()
    {
        TariffsListBox.ItemsSource = _app.Map.GetAllZonesWithAvailability()
            .Select(zone => $"[{zone.Code}] {zone.Name} — {zone.HourlyRate:N0} UZS/soat | Bo'sh: {zone.AvailableSlots}/{zone.TotalSlots} | {zone.Address}")
            .ToList();
    }

    private void RefreshPaymentsPage()
    {
        var user = _app.CurrentUser.CurrentUser;
        if (user?.Role == UserRole.User)
        {
            var payments = _app.User.GetUserPayments(user.Id);
            PaymentsSummaryText.Text = $"Sizning to'lovlaringiz: {payments.Count} ta | Jami: {payments.Sum(p => p.Amount):N0} UZS";
            AllPaymentsListBox.ItemsSource = payments
                .Select(p => $"{p.Amount:N0} UZS | {UiLabels.FormatPaymentStatus(p.Status)} | {p.PaidAtUtc:dd.MM.yyyy HH:mm}")
                .ToList();
            return;
        }

        var allPayments = _app.Query.GetPayments();
        PaymentsSummaryText.Text = user is null
            ? "Barcha to'lovlar (mehmon rejimi — faqat ko'rish)"
            : $"Barcha to'lovlar: {allPayments.Count} ta | Jami: {allPayments.Where(p => p.Status == Domain.Enums.PaymentStatus.Paid).Sum(p => p.Amount):N0} UZS";
        AllPaymentsListBox.ItemsSource = allPayments
            .Select(p => $"{p.Amount:N0} UZS | {UiLabels.FormatPaymentStatus(p.Status)} | Sessiya: {p.SessionId.ToString()[..8]}...")
            .ToList();
    }

    private void RefreshReportsPage()
    {
        var overview = _app.Dashboard.GetOverview();
        ReportsSummaryText.Text =
            $"Hududlar: {overview.TotalZones} | Joylar: {overview.TotalSlots} | Bandlik: {overview.OccupancyPercent}%\n" +
            $"Faol sessiyalar: {overview.ActiveSessions} | Bronlar: {overview.ActiveReservations} | Ochiq muammolar: {overview.OpenProblems}\n" +
            $"Bugungi daromad: {overview.TodayRevenue:N0} UZS | Jami daromad: {overview.TotalRevenue:N0} UZS";

        var items = new List<string>();
        foreach (var zone in overview.ZoneStats)
        {
            items.Add($"[{zone.Code}] {zone.Name}: {zone.Occupied}/{zone.Total} band ({zone.OccupancyPercent}%)");
        }

        foreach (var day in overview.Last7DaysRevenue)
        {
            items.Add($"Daromad {day.Date:dd.MM.yyyy}: {day.Amount:N0} UZS");
        }

        ReportsDetailListBox.ItemsSource = items;
    }

    private void RefreshAllSessionsPage()
    {
        AllSessionsListBox.ItemsSource = _app.Query.GetActiveSessions()
            .Select(session =>
            {
                var userName = _app.Query.FindUser(session.UserId)?.Username ?? "?";
                var slotCode = _app.Query.FindSlot(session.SlotId)?.Code ?? "?";
                var vehicle = _app.Query.FindVehicle(session.VehicleId);
                var plate = vehicle?.PlateNumber ?? "?";
                return $"{userName} | {plate} | {slotCode} | Kirish: {session.CheckInUtc:dd.MM HH:mm}";
            })
            .ToList();
    }

    private void RefreshProblemsPage()
    {
        var items = _app.Problems.GetAllReports()
            .Select(problem => new ProblemListItem(problem.Id,
                $"[{problem.Status}] {problem.Title} — {problem.ZoneName} {problem.SlotCode} ({problem.CreatedAtUtc:dd.MM HH:mm})"))
            .ToList();
        AllProblemsListBox.ItemsSource = items;
        ProblemsListBox.ItemsSource = items;
    }

    private void RefreshUsersPage()
    {
        var items = _app.Query.GetAllUsers()
            .Select(u => $"[{UiLabels.Role(u.Role)}] {u.Username} — {u.PhoneNumber} | {(u.IsActive ? "Faol" : "Nofaol")}")
            .ToList();
        AllUsersListBox.ItemsSource = items;
        UsersListBox.ItemsSource = items;
    }

    private void RefreshProblems_Click(object? sender, RoutedEventArgs e) => RefreshProblemsPage();

    private async void ResolveProblem_Click(object? sender, RoutedEventArgs e)
    {
        var admin = _app.CurrentUser.CurrentUser;
        if (admin?.Role != UserRole.Admin)
        {
            SetStatus("Faqat ma'mur muammoni yopadi.");
            return;
        }

        var problemItem = ProblemsListBox.SelectedItem as ProblemListItem
            ?? AllProblemsListBox.SelectedItem as ProblemListItem;
        if (problemItem is null)
        {
            SetStatus("Muammoni tanlang.");
            return;
        }

        var result = _app.Problems.Resolve(admin.Id, problemItem.Id);
        SetStatus(result.Message);
        if (result.Succeeded)
        {
            await PersistAsync();
            RefreshProblemsPage();
            RefreshDashboard();
        }
    }

    private async void SetSlotOutOfService_Click(object? sender, RoutedEventArgs e) =>
        await UpdateSlotStatusAsync(SlotStatus.OutOfService);

    private async void SetSlotAvailable_Click(object? sender, RoutedEventArgs e) =>
        await UpdateSlotStatusAsync(SlotStatus.Available);

    private async Task UpdateSlotStatusAsync(SlotStatus status)
    {
        var admin = _app.CurrentUser.CurrentUser;
        if (admin?.Role != UserRole.Admin)
        {
            SetStatus("Faqat ma'mur park joy holatini o'zgartira oladi.");
            return;
        }

        var result = _app.Admin.SetSlotStatus(admin.Id, AdminSlotCodeBox.Text ?? string.Empty, status);
        SetStatus(result.Message);
        if (result.Succeeded)
        {
            await PersistAsync();
            RefreshAllZones();
            RefreshDashboard();
        }
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
