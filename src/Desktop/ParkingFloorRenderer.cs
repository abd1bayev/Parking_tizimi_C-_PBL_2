using Application.DTOs.Map;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
    private static readonly IBrush OutOfServiceBg = new SolidColorBrush(Color.Parse("#FEE2E2"));
    private static readonly IBrush OutOfServiceBorder = new SolidColorBrush(Color.Parse("#EF4444"));
    private static readonly IBrush SelectedBorder = new SolidColorBrush(Color.Parse("#6366F1"));
    private static readonly IBrush LabelColor = new SolidColorBrush(Color.Parse("#64748B"));
    private static readonly IBrush RowLabelColor = new SolidColorBrush(Color.Parse("#6366F1"));
    private static readonly IBrush EntryBg = new SolidColorBrush(Color.Parse("#DCFCE7"));
    private static readonly IBrush EntryFg = new SolidColorBrush(Color.Parse("#16A34A"));
    private static readonly IBrush ExitBg = new SolidColorBrush(Color.Parse("#FEE2E2"));
    private static readonly IBrush ExitFg = new SolidColorBrush(Color.Parse("#DC2626"));
    private static readonly IBrush AisleColor = new SolidColorBrush(Color.Parse("#E2E8F0"));

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

        var wrapper = new StackPanel { Spacing = 20 };
        var rowGroups = GroupByRow(slots);

        foreach (var (rowLabel, rowSlots) in rowGroups)
        {
            wrapper.Children.Add(CreateRowBlock(rowLabel, rowSlots, selectedSlotCode, slotClicked));
        }

        var legend = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 16,
            Margin = new Thickness(0, 8, 0, 0)
        };
        legend.Children.Add(CreateLegendItem("Bosh", AvailableBorder));
        legend.Children.Add(CreateLegendItem("Bron", ReservedBorder));
        legend.Children.Add(CreateLegendItem("Band", new SolidColorBrush(Color.Parse("#334155"))));
        legend.Children.Add(CreateLegendItem("Ta'mirda", OutOfServiceBorder));
        wrapper.Children.Add(legend);

        var entryExit = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
            Margin = new Thickness(0, 4, 0, 0)
        };
        var entry = CreateBadge("Kirish", EntryBg, EntryFg);
        Grid.SetColumn(entry, 1);
        var exit = CreateBadge("Chiqish", ExitBg, ExitFg);
        Grid.SetColumn(exit, 2);
        entryExit.Children.Add(entry);
        entryExit.Children.Add(exit);
        wrapper.Children.Add(entryExit);

        container.Children.Add(wrapper);
    }

    private static IEnumerable<(string RowLabel, List<ZoneSlotDto> Slots)> GroupByRow(IReadOnlyList<ZoneSlotDto> slots)
    {
        return slots
            .GroupBy(slot =>
            {
                var dash = slot.Code.IndexOf('-');
                if (dash < 0 || dash >= slot.Code.Length - 1)
                {
                    return "?";
                }

                var suffix = slot.Code[(dash + 1)..];
                return char.IsLetter(suffix[0]) ? suffix[0].ToString() : "?";
            })
            .OrderBy(group => group.Key)
            .Select(group => (group.Key, group.OrderBy(s => s.Code).ToList()));
    }

    private static Border CreateRowBlock(
        string rowLabel,
        List<ZoneSlotDto> rowSlots,
        string? selectedSlotCode,
        EventHandler<PointerPressedEventArgs>? slotClicked)
    {
        var block = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#FAFBFC")),
            BorderBrush = AisleColor,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(16, 14)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*")
        };

        var rowTitle = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#EEF2FF")),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 8),
            Margin = new Thickness(0, 0, 14, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = rowLabel,
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Foreground = RowLabelColor
            }
        };
        Grid.SetColumn(rowTitle, 0);
        grid.Children.Add(rowTitle);

        var slotsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };

        for (var i = 0; i < rowSlots.Count; i++)
        {
            slotsPanel.Children.Add(CreateSlotCell(rowSlots[i], selectedSlotCode, slotClicked, i));
        }

        Grid.SetColumn(slotsPanel, 1);
        grid.Children.Add(slotsPanel);
        block.Child = grid;
        return block;
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
            SlotStatus.OutOfService => (OutOfServiceBg, OutOfServiceBorder),
            _ => (AvailableBg, AvailableBorder)
        };

        var cell = new Border
        {
            Background = bg,
            BorderBrush = isSelected ? SelectedBorder : border,
            BorderThickness = isSelected ? new Thickness(2.5) : new Thickness(1.5),
            CornerRadius = new CornerRadius(12),
            MinHeight = 96,
            MinWidth = 78,
            Tag = slot,
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        var content = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 6,
            Margin = new Thickness(6)
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
                FontSize = 10,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.Parse("#B45309")),
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }
        else if (slot.Status == SlotStatus.OutOfService)
        {
            content.Children.Add(new TextBlock
            {
                Text = "XIZMAT",
                FontSize = 9,
                FontWeight = FontWeight.Bold,
                Foreground = OutOfServiceBorder,
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }

        content.Children.Add(new TextBlock
        {
            Text = slot.Code[(slot.Code.IndexOf('-') + 1)..],
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
            Width = 48,
            Height = 26,
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(carColor),
            Child = new Border
            {
                Width = 18,
                Height = 9,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(Color.Parse("#94A3B8")),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 4, 0, 0)
            }
        };
    }

    private static StackPanel CreateLegendItem(string text, IBrush color) =>
        new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            Children =
            {
                new Border { Width = 12, Height = 12, CornerRadius = new CornerRadius(3), Background = color },
                new TextBlock { Text = text, FontSize = 11, Foreground = LabelColor, VerticalAlignment = VerticalAlignment.Center }
            }
        };

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
