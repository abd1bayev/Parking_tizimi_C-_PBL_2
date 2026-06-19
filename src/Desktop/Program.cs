using Avalonia;
using System;
using Microsoft.Extensions.DependencyInjection;
using AppServices = Application.ParkingAppServices;
using Infrastructure.DependencyInjection;

namespace Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += static (_, eventArgs) =>
        {
            Console.Error.WriteLine("[Parking] Kutilmagan xato:");
            Console.Error.WriteLine(eventArgs.ExceptionObject);
        };

        try
        {
            StartupLog.Write("Servislar tayyorlanmoqda...");
            var rootPath = FindProjectRoot();
            var services = new ServiceCollection();
            services.AddParkingInfrastructure(rootPath);
            var provider = services.BuildServiceProvider();
            var appServices = provider.GetRequiredService<AppServices>();

            StartupLog.Write("Ma'lumotlar yuklanmoqda...");
            appServices.StateStore.InitializeAsync().GetAwaiter().GetResult();
            StartupLog.Write("Ma'lumotlar yuklandi.");

            App.Configure(appServices);

            StartupLog.Write("Avalonia ishga tushmoqda...");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[Parking] Desktop yopildi:");
            Console.Error.WriteLine(ex);
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

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
