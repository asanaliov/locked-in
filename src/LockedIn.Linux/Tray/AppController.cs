using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Threading;
using LockedIn.Core.Services;
using LockedIn.Core.Updates;
using LockedIn.Data;
using LockedIn.Data.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace LockedIn.Linux.Tray;

/// <summary>
/// Owns the tray icon and the app window. Closing the window frees it; tracking carries on until Quit.
/// Desktops without a tray (plain GNOME) simply show no icon: launching the app again reopens the window,
/// and the window itself has a Quit link.
/// </summary>
internal sealed class AppController
{
    private static readonly TimeSpan TooltipRefresh = TimeSpan.FromMinutes(1);

    private readonly IServiceProvider _services;
    private readonly IClassicDesktopStyleApplicationLifetime _desktop;
    private readonly LiveStatusProvider _status;
    private readonly NativeMenuItem _updateItem = new() { IsVisible = false };
    private readonly TrayIcon _tray;
    private readonly DispatcherTimer _tooltipTimer;
    private MainWindow? _window;

    public AppController(IServiceProvider services, SingleInstance instance, IClassicDesktopStyleApplicationLifetime desktop, bool showWindow)
    {
        _services = services;
        _desktop = desktop;
        _status = services.GetRequiredService<LiveStatusProvider>();

        using var icon = AssetLoader.Open(new Uri("avares://locked-in/Assets/logo.png"));
        _tray = new TrayIcon { Icon = new WindowIcon(icon), ToolTipText = "Locked In", Menu = BuildMenu(), IsVisible = true };
        _tray.Clicked += (_, _) => ShowWindow();
        TrayIcon.SetIcons(Application.Current!, [_tray]);

        var updates = services.GetRequiredService<UpdateService>();
        updates.UpdateFound += update => Dispatcher.UIThread.Post(() => OfferUpdate(update));
        instance.ActivateRequested += () => Dispatcher.UIThread.Post(ShowWindow);

        // A tray host only reads the tooltip it was last given, so it's refreshed on a slow timer instead of on hover.
        _tooltipTimer = new DispatcherTimer(TooltipRefresh, DispatcherPriority.Background, async (_, _) => await RefreshTooltipAsync());
        _tooltipTimer.Start();

        desktop.Exit += (_, _) =>
        {
            _tooltipTimer.Stop();
            _tray.IsVisible = false;
            _tray.Dispose();
        };

        if (showWindow)
            ShowWindow();
    }

    private NativeMenu BuildMenu()
    {
        var open = new NativeMenuItem("Open Locked In");
        open.Click += (_, _) => ShowWindow();

        var startAtLogin = new NativeMenuItem("Start at login") { ToggleType = NativeMenuItemToggleType.CheckBox, IsChecked = Autostart.IsEnabled() };
        startAtLogin.Click += (_, _) =>
        {
            Autostart.SetEnabled(!Autostart.IsEnabled());
            startAtLogin.IsChecked = Autostart.IsEnabled();
        };

        var quit = new NativeMenuItem("Quit");
        quit.Click += (_, _) => _desktop.Shutdown();

        _updateItem.Click += (_, _) => OpenLink(ReleaseFeed.LatestReleasePage);

        var menu = new NativeMenu();
        menu.Items.Add(_updateItem);
        menu.Items.Add(open);
        menu.Items.Add(startAtLogin);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(quit);
        return menu;
    }

    private void OfferUpdate(AvailableUpdate update)
    {
        _updateItem.Header = $"Download version {update.Version.ToString(3)}";
        _updateItem.IsVisible = true;
    }

    public void ShowWindow()
    {
        if (_window is null)
        {
            _window = new MainWindow(_services, () => _desktop.Shutdown());
            _window.Closed += (_, _) =>
            {
                _window = null;
                // Let the dispatcher finish tearing the window down, then give its memory back.
                Dispatcher.UIThread.Post(ReleaseWindowMemory, DispatcherPriority.ApplicationIdle);
            };
        }

        _window.Show();
        if (_window.WindowState == WindowState.Minimized)
            _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    private static void ReleaseWindowMemory()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
    }

    private async Task RefreshTooltipAsync()
    {
        try
        {
            var live = await Task.Run(() => _status.GetAsync(CancellationToken.None));
            _tray.ToolTipText = $"Locked In · {LockInStages.For(live.Score).Name} ({live.Score}) · streak {Format.Duration(live.CurrentStreak)}";
        }
        catch (Exception)
        {
            _tray.ToolTipText = "Locked In";
        }
    }

    public static void OpenLink(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })?.Dispose();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            // No browser or xdg-open on this system; nothing useful to do.
        }
    }
}
