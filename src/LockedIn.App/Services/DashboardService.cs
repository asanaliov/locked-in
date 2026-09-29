using LockedIn.Data;
using LockedIn.Data.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LockedIn.App.Services;

public sealed record DaySummary(DateOnly Day, DayMetrics Metrics);

public sealed class DashboardService(
    IDbContextFactory<LockedInDbContext> dbFactory,
    ICurrentSessionSource currentSession,
    IOptionsMonitor<MetricsOptions> options)
{
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    public Task<DayMetrics> GetDayAsync(DateOnly day, CancellationToken cancellationToken) =>
        GetRangeAsync(day, day, cancellationToken);

    /// <summary>Metrics for every day from <paramref name="first"/> to <paramref name="last"/>, inclusive, as one period.</summary>
    public async Task<DayMetrics> GetRangeAsync(DateOnly first, DateOnly last, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var sessions = await db.LoadClippedAsync(
            SessionQueries.UtcRange(first).FromUtc, SessionQueries.UtcRange(last).ToUtc, currentSession, cancellationToken);
        return DayMetricsCalculator.Calculate(sessions, options.CurrentValue.StreakInterruptionTolerance);
    }

    public async Task<double[]> GetMinutesPerHourAsync(DateOnly day, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var (fromUtc, toUtc) = SessionQueries.UtcRange(day);
        var sessions = await db.LoadClippedAsync(fromUtc, toUtc, currentSession, cancellationToken);
        return HourlyUsageCalculator.MinutesPerHour(sessions);
    }

    public async Task<IReadOnlyList<DaySummary>> GetLastDaysAsync(int count, CancellationToken cancellationToken)
    {
        var days = new List<DaySummary>(count);
        for (var day = Today.AddDays(1 - count); day <= Today; day = day.AddDays(1))
            days.Add(new DaySummary(day, await GetDayAsync(day, cancellationToken)));
        return days;
    }
}
