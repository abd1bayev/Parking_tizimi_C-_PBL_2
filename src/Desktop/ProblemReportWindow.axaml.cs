using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Desktop;

public partial class ProblemReportWindow : Window
{
    public string ReportTitle => TitleBox.Text ?? string.Empty;
    public string ReportDescription => DescriptionBox.Text ?? string.Empty;

    public ProblemReportWindow()
    {
        InitializeComponent();
    }

    private void Submit_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleBox.Text) || string.IsNullOrWhiteSpace(DescriptionBox.Text))
        {
            ErrorText.Text = "Sarlavha va tavsif to'ldirilishi shart.";
            return;
        }

        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
}
