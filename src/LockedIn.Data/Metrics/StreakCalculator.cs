namespace LockedIn.Data.Metrics;

/// <summary>
/// A streak is a wall-clock run of Focus time. Anything between two Focus sessions (other apps, idle)
/// shorter than the interruption tolerance is absorbed into the streak.
/// </summary>
public static class StreakCalculator
{
    public static TimeSpan LongestFocusStreak(IEnumerable<UsageSession> sessions, TimeSpan interruptionTolerance) =>
        FocusStreaks(sessions, interruptionTolerance)
            .Select(streak => streak.End - streak.Start)
            .DefaultIfEmpty(TimeSpan.Zero)
            .Max();

    /// <summary>The streak still going at <paramref name="now"/>, or zero if it was interrupted.</summary>
    public static TimeSpan CurrentFocusStreak(IEnumerable<UsageSession> sessions, TimeSpan interruptionTolerance, DateTime now)
    {
        var last = FocusStreaks(sessions, interruptionTolerance).LastOrDefault();
        return now - last.End < interruptionTolerance ? last.End - last.Start : TimeSpan.Zero;
    }

    private static IEnumerable<(DateTime Start, DateTime End)> FocusStreaks(
        IEnumerable<UsageSession> sessions, TimeSpan interruptionTolerance)
    {
        DateTime? start = null;
        var end = DateTime.MinValue;

        foreach (var session in sessions.Where(s => s.Category == Category.Focus).OrderBy(s => s.StartTime))
        {
            if (start is { } streakStart && session.StartTime - end >= interruptionTolerance)
            {
                yield return (streakStart, end);
                start = null;
            }

            if (start is null)
            {
                start = session.StartTime;
                end = session.EndTime;
            }
            else if (session.EndTime > end)
            {
                end = session.EndTime;
            }
        }

        if (start is { } lastStart)
            yield return (lastStart, end);
    }
}
