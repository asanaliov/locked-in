using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Navigation;
using LockedIn.App.Services;
using LockedIn.App.Theming;
using LockedIn.App.Updates;
using LockedIn.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace LockedIn.App;

/// <summary>
/// The dashboard window. Pages are built when opened and read their data once; nothing polls while it's open,
/// and closing the window lets the whole thing be collected while tracking carries on in the tray.
/// </summary>
public partial class MainWindow : Window
{
    private readonly IServiceProvider _services;
    private string _current = "Today";

    public MainWindow(IServiceProvider services)
    {
        _services = services;
        InitializeComponent();
        Version.Text = $"v{typeof(MainWindow).Assembly.GetName().Version?.ToString(3)}";

        ThemeManager.Changed += OnThemeChanged;
        Closed += (_, _) => ThemeManager.Changed -= OnThemeChanged;
        SourceInitialized += (_, _) => ApplyTitleBar();
        ((RadioButton)Nav.Children[0]).IsChecked = true;
    }

    private async void OnNavigate(object sender, RoutedEventArgs e)
    {
        _current = (string)((FrameworkElement)sender).Tag;
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

        Motion.Animate(Page, OpacityProperty, 0, 1, TimeSpan.FromMilliseconds(180));
    }

    /// <summary>Charts and bars take their colours when drawn, so the page is rebuilt with the new palette.</summary>
    private async void OnThemeChanged()
    {
        ApplyTitleBar();
        if (_current != "Settings")
            await ShowPageAsync();
    }

    /// <summary>Colours the title bar like the page (Windows 11; Windows 10 just gets a light or dark bar).</summary>
    private void ApplyTitleBar()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
            return;

        SetAttribute(handle, NativeMethods.DwmwaUseImmersiveDarkMode, ThemeManager.IsDark ? 1 : 0);
        SetAttribute(handle, NativeMethods.DwmwaCaptionColor, ToColorRef("Bg"));
        SetAttribute(handle, NativeMethods.DwmwaBorderColor, ToColorRef("Bg"));
        SetAttribute(handle, NativeMethods.DwmwaTextColor, ToColorRef("Text"));
    }

    private static int ToColorRef(string key)
    {
        var color = ((SolidColorBrush)ThemeManager.Brush(key)).Color;
        return color.R | color.G << 8 | color.B << 16;
    }

    private static void SetAttribute(IntPtr handle, int attribute, int value) =>
        NativeMethods.DwmSetWindowAttribute(handle, attribute, ref value, sizeof(int));

    private void OnLink(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
