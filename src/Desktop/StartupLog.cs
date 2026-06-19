namespace Desktop;

internal static class StartupLog
{
    public static void Write(string message)
    {
        var line = $"[Parking] {DateTime.Now:HH:mm:ss} {message}";
        Console.WriteLine(line);
    }
}
