using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using LockedIn.App.Theming;
using LockedIn.Data;
using LockedIn.Data.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Forms = System.Windows.Forms;

namespace LockedIn.App.Tray;

/// <summary>
/// Owns the tray icon and the app window. Closing the window frees it; tracking carries on in the tray
/// until Exit. The tooltip is refreshed only on hover, so it costs nothing while you work.
/// </summary>
internal sealed class TrayIcon : IDisposable
{
    private static readonly TimeSpan TooltipRefreshThrottle = TimeSpan.FromSeconds(5);

    private readonly IServiceProvider _services;
    private readonly LiveStatusProvider _status;
    private readonly Forms.NotifyIcon _icon;
    private readonly RegisteredWaitHandle _activateWait;
    private MainWindow? _window;
    private DateTime _lastTooltipRefresh = DateTime.MinValue;

    public TrayIcon(IServiceProvider services, SingleInstance instance, bool showWindow)
    {
        _services = services;
        _status = services.GetRequiredService<LiveStatusProvider>();
        ThemeManager.Initialize(services.GetRequiredService<IOptionsMonitor<AppearanceOptions>>());

        _icon = new Forms.NotifyIcon { Icon = AppIcon.Value, Text = "Locked In", Visible = true, ContextMenuStrip = BuildMenu() };
        _icon.MouseMove += async (_, _) => await RefreshTooltipAsync();
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left)
                ShowWindow();
        };

        var dispatcher = Dispatcher.CurrentDispatcher;
        _activateWait = ThreadPool.RegisterWaitForSingleObject(
            instance.ActivateRequested, (_, _) => dispatcher.BeginInvoke(ShowWindow), null, Timeout.Infinite, executeOnlyOnce: false);

        if (showWindow)
            ShowWindow();
    }

    public void Dispose()
    {
        _activateWait.Unregister(null);
        _window?.Close();
        _icon.Visible = false;
        _icon.Dispose();
    }

    private Forms.ContextMenuStrip BuildMenu()
    {
        var startWithWindows = new Forms.ToolStripMenuItem("Start with Windows") { Checked = StartupRegistration.IsEnabled() };
        startWithWindows.Click += (_, _) =>
        {
            StartupRegistration.SetEnabled(!startWithWindows.Checked);
            startWithWindows.Checked = StartupRegistration.IsEnabled();
        };

        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open Locked In", null, (_, _) => ShowWindow());
        menu.Items.Add(startWithWindows);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Application.Current.Shutdown());
        return menu;
    }

    private void ShowWindow()
    {
        if (_window is null)
        {
            _window = new MainWindow(_services);
            _window.Closed += (_, _) =>
            {
                _window = null;
                // Let the dispatcher finish tearing the window down, then give its memory back.
                Application.Current.Dispatcher.BeginInvoke(ReleaseWindowMemory, DispatcherPriority.ApplicationIdle);
            };
        }

        _window.Show();
        if (_window.WindowState == WindowState.Minimized)
            _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    /// <summary>
    /// Most of the day the app only tracks, so once the window is gone its pages, images and caches are collected,
    /// the heap is compacted and handed back to Windows, and the working set is trimmed.
    /// </summary>
    private static void ReleaseWindowMemory()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);
        using var self = Process.GetCurrentProcess();
        NativeMethods.EmptyWorkingSet(self.Handle);
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
            _icon.Text = $"Locked In · {LockInStages.For(live.Score).Name} ({live.Score}) · streak {Format.Duration(live.CurrentStreak)}";
        }
        catch (Exception)
        {
            _icon.Text = "Locked In";
        }
    }
}
