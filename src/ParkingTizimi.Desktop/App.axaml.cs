using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ParkingTizimi.Core.Services;
using ParkingTizimi.Infrastructure.Repositories;
using ParkingTizimi.Shared.Time;

namespace ParkingTizimi.Desktop;

public partial class App : Application
{
    public override void Initialize()
    {
        DesktopLog.Write("App.Initialize called.");
        AvaloniaXamlLoader.Load(this);
        DesktopLog.Write("App.Initialize completed.");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        DesktopLog.Write($"Framework initialization lifetime: {ApplicationLifetime?.GetType().Name ?? "null"}");
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DesktopLog.Write($"Current directory: {Directory.GetCurrentDirectory()}");
            var repository = new JsonParkingRepository(Directory.GetCurrentDirectory());
            var service = new ParkingSystemService(repository, new SystemClock());
            Task.Run(() => service.InitializeAsync()).GetAwaiter().GetResult();
            DesktopLog.Write("Parking service initialized.");
            desktop.MainWindow = new MainWindow(service);
            DesktopLog.Write("MainWindow assigned to desktop lifetime.");
        }

        base.OnFrameworkInitializationCompleted();
        DesktopLog.Write("OnFrameworkInitializationCompleted finished.");
    }
}