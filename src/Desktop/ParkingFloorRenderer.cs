using Application.DTOs.Map;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Domain.Enums;

namespace Desktop;

internal static class ParkingFloorRenderer
{
    private static readonly IBrush AvailableBg = new SolidColorBrush(Color.Parse("#F8FAFC"));
    private static readonly IBrush AvailableBorder = new SolidColorBrush(Color.Parse("#CBD5E1"));
    private static readonly IBrush ReservedBg = new SolidColorBrush(Color.Parse("#FEF3C7"));
    private static readonly IBrush ReservedBorder = new SolidColorBrush(Color.Parse("#F59E0B"));
    private static readonly IBrush OccupiedBg = new SolidColorBrush(Color.Parse("#E2E8F0"));
    private static readonly IBrush SelectedBorder = new SolidColorBrush(Color.Parse("#6366F1"));
    private static readonly IBrush LabelColor = new SolidColorBrush(Color.Parse("#64748B"));
    private static readonly IBrush EntryBg = new SolidColorBrush(Color.Parse("#DCFCE7"));
    private static readonly IBrush EntryFg = new SolidColorBrush(Color.Parse("#16A34A"));
    private static readonly IBrush ExitBg = new SolidColorBrush(Color.Parse("#FEE2E2"));
    private static readonly IBrush ExitFg = new SolidColorBrush(Color.Parse("#DC2626"));

    private static readonly Color[] CarColors =
    [
        Color.Parse("#1E293B"),
        Color.Parse("#DC2626"),
        Color.Parse("#2563EB"),
        Color.Parse("#64748B"),
        Color.Parse("#0F766E")
    ];

    public static void Render(
        Panel container,
        IReadOnlyList<ZoneSlotDto> slots,
        string? selectedSlotCode,
        EventHandler<PointerPressedEventArgs>? slotClicked)
    {
        container.Children.Clear();

        var wrapper = new Grid
        {
            RowDefinitions = new RowDefinitions("*,Auto,Auto"),
            Margin = new Thickness(8)
        };

        var columns = 3;
        var rows = (int)Math.Ceiling(slots.Count / (double)columns);
        var blocks = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        for (var c = 0; c < columns; c++)
        {
            blocks.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        for (var r = 0; r < rows; r++)
        {
            blocks.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }

        for (var i = 0; i < slots.Count; i++)
        {
            var cell = CreateSlotCell(slots[i], selectedSlotCode, slotClicked, i);
            Grid.SetRow(cell, i / columns);
            Grid.SetColumn(cell, i % columns);
            blocks.Children.Add(cell);
        }

        wrapper.Children.Add(blocks);
        Grid.SetRow(blocks, 0);

        var entryExit = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
            Margin = new Thickness(0, 16, 0, 0)
        };

        var entry = CreateBadge("Kirish", EntryBg, EntryFg);
        Grid.SetColumn(entry, 1);
        var exit = CreateBadge("Chiqish", ExitBg, ExitFg);
        Grid.SetColumn(exit, 2);
        entryExit.Children.Add(entry);
        entryExit.Children.Add(exit);
        wrapper.Children.Add(entryExit);
        Grid.SetRow(entryExit, 2);

        container.Children.Add(wrapper);
    }

    private static Border CreateSlotCell(
        ZoneSlotDto slot,
        string? selectedSlotCode,
        EventHandler<PointerPressedEventArgs>? slotClicked,
        int index)
    {
        var isSelected = string.Equals(slot.Code, selectedSlotCode, StringComparison.OrdinalIgnoreCase);
        var (bg, border) = slot.Status switch
        {
            SlotStatus.Available => (AvailableBg, AvailableBorder),
            SlotStatus.Reserved => (ReservedBg, ReservedBorder),
            SlotStatus.Occupied => (OccupiedBg, AvailableBorder),
            _ => (AvailableBg, AvailableBorder)
        };

        var cell = new Border
        {
            Background = bg,
            BorderBrush = isSelected ? SelectedBorder : border,
            BorderThickness = isSelected ? new Thickness(2.5) : new Thickness(1.5),
            CornerRadius = new CornerRadius(12),
            Margin = new Thickness(8),
            MinHeight = 110,
            MinWidth = 90,
            Tag = slot,
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        var content = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 6
        };

        if (slot.Status == SlotStatus.Occupied)
        {
            content.Children.Add(CreateCarShape(index));
        }
        else if (slot.Status == SlotStatus.Reserved)
        {
            content.Children.Add(new TextBlock
            {
                Text = "BRON",
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.Parse("#B45309")),
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }

        content.Children.Add(new TextBlock
        {
            Text = slot.Code,
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            Foreground = LabelColor,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        cell.Child = content;
        if (slotClicked is not null)
        {
            cell.PointerPressed += slotClicked;
        }

        return cell;
    }

    private static Border CreateCarShape(int index)
    {
        var carColor = CarColors[index % CarColors.Length];
        return new Border
        {
            Width = 52,
            Height = 28,
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(carColor),
            Child = new Border
            {
                Width = 20,
                Height = 10,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(Color.Parse("#94A3B8")),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 4, 0, 0)
            }
        };
    }

    private static Border CreateBadge(string text, IBrush bg, IBrush fg) =>
        new()
        {
            Background = bg,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 6),
            Margin = new Thickness(6, 0, 0, 0),
            Child = new TextBlock
            {
                Text = text,
                Foreground = fg,
                FontWeight = FontWeight.SemiBold,
                FontSize = 12
            }
        };
}
