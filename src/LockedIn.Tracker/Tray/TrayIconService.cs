using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using LockedIn.Data;
using Microsoft.Extensions.Options;

namespace LockedIn.Tracker.Tray;

/// <summary>
/// Tray icon on its own UI thread. The tooltip is refreshed only when the mouse hovers over it,
/// so it costs nothing while you work.
/// </summary>
public sealed class TrayIconService(
    LiveStatusProvider status,
    IHostApplicationLifetime lifetime,
    IOptions<TrackerOptions> options,
    ILogger<TrayIconService> logger) : IHostedService
{
    private static readonly TimeSpan RefreshThrottle = TimeSpan.FromSeconds(5);

    private readonly TaskCompletionSource<SynchronizationContext> _uiContext = new();
    private DateTime _lastRefresh = DateTime.MinValue;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var thread = new Thread(RunMessageLoop) { IsBackground = true, Name = "Tray" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var context = await _uiContext.Task.WaitAsync(cancellationToken);
        context.Post(_ => Application.ExitThread(), null);
    }

    private void RunMessageLoop()
    {
        Application.EnableVisualStyles();
        SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());

        using var icon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? SystemIcons.Application,
            Text = "locked-in",
            Visible = true,
            ContextMenuStrip = BuildMenu(),
        };
        icon.MouseMove += async (_, _) => await RefreshTooltipAsync(icon);
        icon.DoubleClick += (_, _) => OpenDashboard();

        _uiContext.SetResult(SynchronizationContext.Current!);
        Application.Run();
        icon.Visible = false;
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
        menu.Items.Add("Open dashboard", null, (_, _) => OpenDashboard());
        menu.Items.Add(startWithWindows);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => lifetime.StopApplication());
        return menu;
    }

    private async Task RefreshTooltipAsync(NotifyIcon icon)
    {
        if (DateTime.UtcNow - _lastRefresh < RefreshThrottle)
            return;
        _lastRefresh = DateTime.UtcNow;

        try
        {
            // Off the UI thread so the database call never blocks the message loop.
            var live = await Task.Run(() => status.GetAsync(lifetime.ApplicationStopping));
            icon.Text = $"locked-in · score {live.Score} · streak {Format.Duration(live.CurrentStreak)}";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not refresh the tray tooltip");
        }
    }

    private void OpenDashboard() =>
        Process.Start(new ProcessStartInfo(options.Value.DashboardUrl) { UseShellExecute = true });
}
