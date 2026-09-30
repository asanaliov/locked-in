using System.Windows;
using System.Windows.Controls;
using LockedIn.App.Services;
using LockedIn.App.Theming;

namespace LockedIn.App.Views;

public partial class AppsPage : UserControl, IPage
{
    private readonly DashboardService _dashboard;
    private int _days = 7;

    public AppsPage(DashboardService dashboard)
    {
        _dashboard = dashboard;
        InitializeComponent();
    }

    public async Task LoadAsync()
    {
        var today = DashboardService.Today;
        var metrics = await _dashboard.GetRangeAsync(today.AddDays(1 - _days), today, CancellationToken.None);
        List.ItemsSource = AppRow.From(metrics.Apps, metrics.ActiveTime, withCategoryAndShare: true);
        Empty.Visibility = metrics.Apps.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void OnPeriodChecked(object sender, RoutedEventArgs e)
    {
        _days = int.Parse((string)((FrameworkElement)sender).Tag);
        if (IsLoaded)
            await LoadAsync();
    }
}
