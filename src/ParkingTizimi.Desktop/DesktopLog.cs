using System;
using System.IO;
using System.Text;

namespace ParkingTizimi.Desktop;

internal static class DesktopLog
{
    private static readonly object Sync = new();

    public static string LogPath
    {
        get
        {
            var directory = Path.Combine(Directory.GetCurrentDirectory(), "data");
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, "desktop-startup.log");
        }
    }

    public static void Write(string message)
    {
        lock (Sync)
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}", Encoding.UTF8);
        }
    }
}