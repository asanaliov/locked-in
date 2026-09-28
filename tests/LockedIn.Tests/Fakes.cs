using LockedIn.Data;
using LockedIn.Data.Classification;
using LockedIn.Tracker;
using LockedIn.Tracker.Sessions;
using LockedIn.Tracker.Windows;

namespace LockedIn.Tests;

internal sealed class FakeClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc);

    public void Advance(TimeSpan by) => UtcNow += by;
}

internal sealed class FakeWindows : IActiveWindowProvider
{
    public ActiveWindow? Current { get; set; }

    public ActiveWindow? GetActiveWindow() => Current;
}

internal sealed class FakeIdleDetector : IIdleDetector
{
    public TimeSpan IdleTime { get; set; }

    public TimeSpan GetIdleTime() => IdleTime;
}

internal sealed class FakeOverrides : ICategoryOverrides
{
    public Dictionary<string, Category> Overrides { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Category? Find(string appName) => Overrides.TryGetValue(appName, out var category) ? category : null;
}

internal sealed class InMemorySessionStore : ISessionStore
{
    public List<UsageSession> Saved { get; } = [];

    public Task SaveAsync(UsageSession session, CancellationToken cancellationToken)
    {
        Saved.Add(session);
        return Task.CompletedTask;
    }
}

internal static class Sessions
{
    public static readonly DateTime Day = new(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc);

    /// <summary>Builds a session from minute offsets relative to <see cref="Day"/>.</summary>
    public static UsageSession At(double startMinute, double endMinute, Category category, string app = "app") => new()
    {
        AppName = app,
        Category = category,
        StartTime = Day.AddMinutes(startMinute),
        EndTime = Day.AddMinutes(endMinute),
    };
}
