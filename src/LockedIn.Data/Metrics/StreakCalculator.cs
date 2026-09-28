namespace LockedIn.Data.Metrics;

public static class StreakCalculator
{
    /// <summary>
    /// Longest wall-clock run of Focus time. Anything between two Focus sessions (other apps, idle)
    /// shorter than <paramref name="interruptionTolerance"/> is absorbed into the streak.
    /// </summary>
    public static TimeSpan LongestFocusStreak(IEnumerable<UsageSession> sessions, TimeSpan interruptionTolerance)
    {
        var longest = TimeSpan.Zero;
        DateTime? streakStart = null;
        var streakEnd = DateTime.MinValue;

        foreach (var session in sessions.Where(s => s.Category == Category.Focus).OrderBy(s => s.StartTime))
        {
            if (streakStart is null || session.StartTime - streakEnd >= interruptionTolerance)
                streakStart = session.StartTime;

            if (session.EndTime > streakEnd)
                streakEnd = session.EndTime;

            var length = streakEnd - streakStart.Value;
            if (length > longest)
                longest = length;
        }

        return longest;
    }
}
