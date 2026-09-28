using LockedIn.Data;
using LockedIn.Data.Classification;
using LockedIn.Tracker.Windows;
using Microsoft.Extensions.Options;

namespace LockedIn.Tracker.Sessions;

/// <summary>
/// Keeps the current session in memory and only writes it out when it ends:
/// on an app or category change, when the user goes idle, or on shutdown.
/// </summary>
public sealed class SessionTracker(
    IActiveWindowProvider windows,
    IIdleDetector idleDetector,
    IAppClassifier classifier,
    IClock clock,
    ISessionStore store,
    IOptions<TrackerOptions> options) : ICurrentSessionSource
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

        var category = classifier.Classify(window.AppName, window.Title);
        if (_current?.AppName == window.AppName && _current.Category == category)
            return;

        await CloseCurrentAsync(now, cancellationToken);
        _current = new UsageSession
        {
            AppName = window.AppName,
            Category = category,
            WindowTitle = _options.StoreWindowTitles ? window.Title : null,
            StartTime = now,
        };
    }

    /// <summary>Safe to call from another thread.</summary>
    public UsageSession? SnapshotCurrent()
    {
        if (Volatile.Read(ref _current) is not { } session)
            return null;

        return new UsageSession
        {
            AppName = session.AppName,
            Category = session.Category,
            StartTime = session.StartTime,
            EndTime = clock.UtcNow,
        };
    }

    public Task FlushAsync(CancellationToken cancellationToken) => CloseCurrentAsync(clock.UtcNow, cancellationToken);

    private async Task CloseCurrentAsync(DateTime endTime, CancellationToken cancellationToken)
    {
        // Atomic, because a Windows shutdown can flush from another thread mid-tick.
        if (Interlocked.Exchange(ref _current, null) is not { } session)
            return;

        if (endTime <= session.StartTime)
            return; // nothing measurable happened

        session.EndTime = endTime;
        await store.SaveAsync(session, cancellationToken);
    }
}
