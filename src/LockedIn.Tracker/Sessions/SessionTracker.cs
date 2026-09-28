using LockedIn.Data;
using LockedIn.Tracker.Windows;
using Microsoft.Extensions.Options;

namespace LockedIn.Tracker.Sessions;

/// <summary>
/// Keeps the current session in memory and only writes it out when it ends:
/// on an app change, when the user goes idle, or on shutdown.
/// </summary>
public sealed class SessionTracker(
    IActiveWindowProvider windows,
    IIdleDetector idleDetector,
    IClock clock,
    ISessionStore store,
    IOptions<TrackerOptions> options)
{
    private readonly TrackerOptions _options = options.Value;
    private UsageSession? _current;
    private DateTime? _lastTick;

    public async Task TickAsync(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;

        // A long gap between ticks means the machine slept; the session ended when we stopped ticking.
        if (_lastTick is { } lastTick && now - lastTick > _options.IdleThreshold)
            await CloseCurrentAsync(lastTick, cancellationToken);
        _lastTick = now;

        var idleTime = idleDetector.GetIdleTime();
        if (idleTime > _options.IdleThreshold)
        {
            await CloseCurrentAsync(now - idleTime, cancellationToken);
            return;
        }

        var window = windows.GetActiveWindow();
        if (window is null)
        {
            await CloseCurrentAsync(now, cancellationToken);
            return;
        }

        if (_current?.AppName == window.AppName)
            return;

        await CloseCurrentAsync(now, cancellationToken);
        _current = new UsageSession
        {
            AppName = window.AppName,
            WindowTitle = _options.StoreWindowTitles ? window.Title : null,
            StartTime = now,
        };
    }

    public Task FlushAsync(CancellationToken cancellationToken) => CloseCurrentAsync(clock.UtcNow, cancellationToken);

    private async Task CloseCurrentAsync(DateTime endTime, CancellationToken cancellationToken)
    {
        if (_current is not { } session)
            return;

        _current = null;
        if (endTime <= session.StartTime)
            return; // nothing measurable happened

        session.EndTime = endTime;
        await store.SaveAsync(session, cancellationToken);
    }
}
