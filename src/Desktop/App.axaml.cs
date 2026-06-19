using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AppServices = Application.ParkingAppServices;

namespace Desktop;

public partial class App : Avalonia.Application
{
    public static AppServices Services { get; private set; } = null!;

    public static void Configure(AppServices services) => Services = services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            StartupLog.Write("Oyna ochilmoqda...");
            var mainWindow = new MainWindow(Services);
            desktop.MainWindow = mainWindow;
            mainWindow.Show();
            mainWindow.Activate();
            StartupLog.Write("Oyna ochildi.");
        }

        base.OnFrameworkInitializationCompleted();
    }
}
