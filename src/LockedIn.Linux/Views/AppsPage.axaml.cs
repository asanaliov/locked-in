using Avalonia.Controls;
using Avalonia.Interactivity;
using LockedIn.Core.Services;

namespace LockedIn.Linux.Views;

public partial class AppsPage : UserControl, IPage
{
    private readonly DashboardService _dashboard;
    private int _days = 7;

    public AppsPage(DashboardService dashboard)
    {
        _dashboard = dashboard;
        InitializeComponent();

        foreach (var period in Periods.Children.OfType<RadioButton>())
            period.IsCheckedChanged += OnPeriodChanged;
    }

    public async Task LoadAsync()
    {
        var today = DashboardService.Today;
        var metrics = await _dashboard.GetRangeAsync(today.AddDays(1 - _days), today, CancellationToken.None);
        List.ItemsSource = AppRow.From(metrics.Apps, metrics.ActiveTime, withCategoryAndShare: true);
        Empty.IsVisible = metrics.Apps.Count == 0;
    }

    private async void OnPeriodChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { IsChecked: true } period)
            return;

        _days = int.Parse((string)period.Tag!);
        await LoadAsync();
    }
}
