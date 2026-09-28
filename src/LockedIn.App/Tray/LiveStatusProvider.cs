using LockedIn.Data;
using LockedIn.Data.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LockedIn.App.Tray;

public sealed record LiveStatus(int Score, TimeSpan CurrentStreak);

/// <summary>Today's score and the running streak, including the session that isn't saved yet.</summary>
public sealed class LiveStatusProvider(
    IDbContextFactory<LockedInDbContext> dbFactory,
    ICurrentSessionSource currentSession,
    IOptions<MetricsOptions> options)
{
    public async Task<LiveStatus> GetAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var dayStart = SessionQueries.UtcRange(DateOnly.FromDateTime(DateTime.Now)).FromUtc;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var sessions = await db.LoadClippedAsync(dayStart, now, currentSession, cancellationToken);

        var tolerance = options.Value.StreakInterruptionTolerance;
        var today = DayMetricsCalculator.Calculate(sessions, tolerance);
        return new LiveStatus(today.Score, StreakCalculator.CurrentFocusStreak(sessions, tolerance, now));
    }
}
