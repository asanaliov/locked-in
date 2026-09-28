using LockedIn.Data;
using Microsoft.Extensions.Options;

namespace LockedIn.App.Tray;

/// <summary>
/// Owns the tray icon and the app window. Closing the window frees it; tracking carries on in the tray
/// until Exit. The tooltip is refreshed only on hover, so it costs nothing while you work.
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private static readonly TimeSpan TooltipRefreshThrottle = TimeSpan.FromSeconds(5);

    private readonly string _dashboardUrl;
    private readonly LiveStatusProvider _status;
    private readonly IOptionsMonitor<AppearanceOptions> _appearance;
    private readonly NotifyIcon _icon;
    private readonly RegisteredWaitHandle _activateWait;
    private MainWindow? _window;
    private DateTime _lastTooltipRefresh = DateTime.MinValue;

    public TrayApplicationContext(IServiceProvider services, string dashboardUrl, SingleInstance instance, bool showWindow)
    {
        _dashboardUrl = dashboardUrl;
        _status = services.GetRequiredService<LiveStatusProvider>();
        _appearance = services.GetRequiredService<IOptionsMonitor<AppearanceOptions>>();

        // Creating the menu installs the WinForms synchronization context captured below.
        var menu = BuildMenu();
        var ui = SynchronizationContext.Current!;

        _icon = new NotifyIcon { Icon = AppIcon.Value, Text = "Locked In", Visible = true, ContextMenuStrip = menu };
        _icon.MouseMove += async (_, _) => await RefreshTooltipAsync();
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                ShowWindow();
        };

        _activateWait = ThreadPool.RegisterWaitForSingleObject(
            instance.ActivateRequested, (_, _) => ui.Post(_ => ShowWindow(), null), null, Timeout.Infinite, executeOnlyOnce: false);

        if (showWindow)
            ShowWindow();
    }

    protected override void ExitThreadCore()
    {
        _activateWait.Unregister(null);
        _window?.Close();
        _icon.Visible = false;
        _icon.Dispose();
        base.ExitThreadCore();
    }

    private ContextMenuStrip BuildMenu()
    {
        var startWithWindows = new ToolStripMenuItem("Start with Windows") { Checked = StartupRegistration.IsEnabled() };
        startWithWindows.Click += (_, _) =>
        {
            StartupRegistration.SetEnabled(!startWithWindows.Checked);
            startWithWindows.Checked = StartupRegistration.IsEnabled();
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add("Open Locked In", null, (_, _) => ShowWindow());
        menu.Items.Add(startWithWindows);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());
        return menu;
    }

    private void ShowWindow()
    {
        if (_window is null || _window.IsDisposed)
            _window = new MainWindow(_dashboardUrl, _appearance);

        _window.Show();
        if (_window.WindowState == FormWindowState.Minimized)
            _window.WindowState = FormWindowState.Normal;
        _window.Activate();
    }

    private async Task RefreshTooltipAsync()
    {
        if (DateTime.UtcNow - _lastTooltipRefresh < TooltipRefreshThrottle)
            return;
        _lastTooltipRefresh = DateTime.UtcNow;

        try
        {
            // Off the UI thread so the database call never blocks the message loop.
            var live = await Task.Run(() => _status.GetAsync(CancellationToken.None));
            _icon.Text = $"Locked In · score {live.Score} · streak {Format.Duration(live.CurrentStreak)}";
        }
        catch (Exception)
        {
            _icon.Text = "Locked In";
        }
    }
}
