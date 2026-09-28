using Microsoft.EntityFrameworkCore;

namespace LockedIn.Data.Metrics;

public static class SessionQueries
{
    /// <summary>UTC bounds of a calendar day in the machine's local time zone.</summary>
    public static (DateTime FromUtc, DateTime ToUtc) UtcRange(DateOnly localDay) =>
        (ToUtc(localDay), ToUtc(localDay.AddDays(1)));

    /// <summary>Sessions overlapping the range, trimmed so they don't stick out of it.</summary>
    public static async Task<List<UsageSession>> LoadClippedAsync(
        this LockedInDbContext db, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var sessions = await db.UsageSessions
            .AsNoTracking()
            .Where(s => s.StartTime < toUtc && s.EndTime > fromUtc)
            .OrderBy(s => s.StartTime)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            if (session.StartTime < fromUtc) session.StartTime = fromUtc;
            if (session.EndTime > toUtc) session.EndTime = toUtc;
        }
        return sessions;
    }

    private static DateTime ToUtc(DateOnly localDay) =>
        localDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Local).ToUniversalTime();
}
