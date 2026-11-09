using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using LockedIn.Core.Services;
using LockedIn.Core.Updates;
using LockedIn.Linux.Theming;
using LockedIn.Linux.Tray;
using LockedIn.Linux.Views;
using Microsoft.Extensions.DependencyInjection;

namespace LockedIn.Linux;

/// <summary>
/// The dashboard window. Pages are built when opened and read their data once; nothing polls while it's open,
/// and closing the window lets the whole thing be collected while tracking carries on.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>Space kept between the window and the screen edges when the screen is smaller than the window.</summary>
    private const double ScreenMargin = 16;

    private readonly IServiceProvider _services;
    private readonly Action _quit;
    private string _current = "Today";

    public MainWindow(IServiceProvider services, Action quit)
    {
        _services = services;
        _quit = quit;
        InitializeComponent();
        Version.Text = $"v{UpdateService.CurrentVersion.ToString(3)}";

        FitToScreen();
        Classes.Set("animated", ThemeManager.Animations);
        ThemeManager.Changed += OnThemeChanged;
        Closed += (_, _) => ThemeManager.Changed -= OnThemeChanged;

        foreach (var button in Nav.Children.OfType<RadioButton>())
            button.IsCheckedChanged += OnNavigate;
        ((RadioButton)Nav.Children[0]).IsChecked = true;
    }

    private async void OnNavigate(object? sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { IsChecked: true } button)
            return;

        _current = (string)button.Tag!;
        await ShowPageAsync();
    }

    private async Task ShowPageAsync()
    {
        IPage page = _current switch
        {
            "ScreenTime" => new ScreenTimePage(_services.GetRequiredService<DashboardService>()),
            "History" => new HistoryPage(_services.GetRequiredService<DashboardService>()),
            "Apps" => new AppsPage(_services.GetRequiredService<DashboardService>()),
            "Settings" => new SettingsPage(
                _services.GetRequiredService<GeneralSettingsService>(),
                _services.GetRequiredService<CategorySettingsService>(),
                _services.GetRequiredService<UpdateService>()),
            _ => new TodayPage(_services.GetRequiredService<DashboardService>()),
        };

        Page.Content = page;
        await page.LoadAsync();
    }

    /// <summary>Charts and bars take their colours when drawn, so the page is rebuilt with the new palette.</summary>
    private async void OnThemeChanged()
    {
        Classes.Set("animated", ThemeManager.Animations);
        if (_current != "Settings")
            await ShowPageAsync();
    }

    /// <summary>Shrinks the window to fit a screen with less usable room than its default size, as on a small laptop.</summary>
    private void FitToScreen()
    {
        if ((Screens.ScreenFromWindow(this) ?? Screens.Primary) is not { } screen)
            return;

        var area = screen.WorkingArea.ToRect(screen.Scaling);
        Width = Math.Min(Width, area.Width - 2 * ScreenMargin);
        Height = Math.Min(Height, area.Height - 2 * ScreenMargin);
    }

    private void OnLink(object? sender, RoutedEventArgs e) => AppController.OpenLink((string)((Control)sender!).Tag!);

    private void OnQuit(object? sender, RoutedEventArgs e) => _quit();
}
