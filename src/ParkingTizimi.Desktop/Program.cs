using Avalonia;
using System;

namespace ParkingTizimi.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            DesktopLog.Write($"Unhandled exception: {eventArgs.ExceptionObject}");
        };

        try
        {
            DesktopLog.Write("Program.Main started.");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            DesktopLog.Write("Program.Main finished.");
        }
        catch (Exception exception)
        {
            DesktopLog.Write($"Fatal startup exception: {exception}");
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
