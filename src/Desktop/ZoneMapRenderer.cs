using Application.DTOs.Map;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;

namespace Desktop;

internal static class ZoneMapRenderer
{
    private static readonly IBrush DefaultFill = new SolidColorBrush(Color.Parse("#6366F1"));
    private static readonly IBrush SelectedFill = new SolidColorBrush(Color.Parse("#4338CA"));
    private static readonly IBrush LabelBrush = new SolidColorBrush(Color.Parse("#0F172A"));

    public static void Render(Canvas canvas, IReadOnlyList<ZoneAvailabilityDto> zones, Guid? selectedZoneId)
    {
        canvas.Children.Clear();
        if (zones.Count == 0)
        {
            return;
        }

        var width = canvas.Bounds.Width;
        var height = canvas.Bounds.Height;
        if (width <= 0 || height <= 0)
        {
            width = 520;
            height = 320;
        }

        var minLat = zones.Min(z => z.Latitude) - 0.01;
        var maxLat = zones.Max(z => z.Latitude) + 0.01;
        var minLng = zones.Min(z => z.Longitude) - 0.01;
        var maxLng = zones.Max(z => z.Longitude) + 0.01;

        foreach (var zone in zones)
        {
            var x = MapValue(zone.Longitude, minLng, maxLng, 40, width - 40);
            var y = MapValue(zone.Latitude, maxLat, minLat, 30, height - 30);
            var occupancy = zone.TotalSlots == 0 ? 0 : (double)zone.OccupiedSlots / zone.TotalSlots;
            var radius = 14 + occupancy * 10;
            var isSelected = zone.ZoneId == selectedZoneId;

            var dot = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = isSelected ? SelectedFill : GetAvailabilityBrush(zone),
                Stroke = isSelected ? SelectedFill : DefaultFill,
                StrokeThickness = isSelected ? 3 : 1.5,
                Tag = zone.ZoneId
            };

            Canvas.SetLeft(dot, x - radius);
            Canvas.SetTop(dot, y - radius);
            canvas.Children.Add(dot);

            var label = new TextBlock
            {
                Text = zone.Code,
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Foreground = LabelBrush
            };
            Canvas.SetLeft(label, x - 14);
            Canvas.SetTop(label, y + radius + 4);
            canvas.Children.Add(label);
        }
    }

    public static Guid? HitTest(Canvas canvas, Point point)
    {
        foreach (var child in canvas.Children.OfType<Ellipse>())
        {
            var left = Canvas.GetLeft(child);
            var top = Canvas.GetTop(child);
            var center = new Point(left + child.Width / 2, top + child.Height / 2);
            var distance = Math.Sqrt(Math.Pow(point.X - center.X, 2) + Math.Pow(point.Y - center.Y, 2));
            if (distance <= child.Width / 2 + 4 && child.Tag is Guid zoneId)
            {
                return zoneId;
            }
        }

        return null;
    }

    private static IBrush GetAvailabilityBrush(ZoneAvailabilityDto zone)
    {
        if (zone.TotalSlots == 0)
        {
            return new SolidColorBrush(Color.Parse("#94A3B8"));
        }

        var ratio = (double)zone.AvailableSlots / zone.TotalSlots;
        return ratio switch
        {
            >= 0.6 => new SolidColorBrush(Color.Parse("#22C55E")),
            >= 0.3 => new SolidColorBrush(Color.Parse("#F59E0B")),
            _ => new SolidColorBrush(Color.Parse("#EF4444"))
        };
    }

    private static double MapValue(double value, double min, double max, double outMin, double outMax)
    {
        if (Math.Abs(max - min) < 0.0001)
        {
            return (outMin + outMax) / 2;
        }

        return outMin + (value - min) / (max - min) * (outMax - outMin);
    }
}
