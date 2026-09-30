namespace LockedIn.Data.Metrics;

public static class DayMetricsCalculator
{
    /// <summary>Computes metrics for the sessions of one period. Sessions must already be clipped to it.</summary>
    public static DayMetrics Calculate(IReadOnlyCollection<UsageSession> sessions, TimeSpan streakTolerance)
    {
        var ordered = sessions.OrderBy(s => s.StartTime).ToList();
        var active = Sum(ordered);
        var focus = Sum(ordered.Where(s => s.Category == Category.Focus));
        var streak = StreakCalculator.LongestFocusStreak(ordered, streakTolerance);
        var switchesPerHour = active > TimeSpan.Zero ? CountContextSwitches(ordered) / active.TotalHours : 0;
        var focusRatio = active > TimeSpan.Zero ? focus / active : 0;
        var score = active > TimeSpan.Zero ? LockedInScoreCalculator.Calculate(focusRatio, streak, switchesPerHour) : 0;

        return new DayMetrics(
            ActiveTime: active,
            FocusTime: focus,
            NeutralTime: Sum(ordered.Where(s => s.Category == Category.Neutral)),
            DistractionTime: Sum(ordered.Where(s => s.Category == Category.Distraction)),
            LongestStreak: streak,
            SwitchesPerHour: switchesPerHour,
            Score: score,
            Apps: TimePerApp(ordered));
    }

    /// <summary>A context switch is moving from one app to a different one.</summary>
    public static int CountContextSwitches(IReadOnlyList<UsageSession> ordered) =>
        ordered.Zip(ordered.Skip(1)).Count(pair => pair.First.AppName != pair.Second.AppName);

    private static readonly Category[] SplitOrder = [Category.Focus, Category.Neutral, Category.Distraction];

    private static IReadOnlyList<AppUsage> TimePerApp(IEnumerable<UsageSession> sessions) =>
        sessions
            .GroupBy(s => s.AppName)
            .Select(g =>
            {
                // A browser can be both Focus and Distraction, so keep the split as well as the main category.
                var split = SplitOrder
                    .Select(category => new CategoryTime(category, Sum(g.Where(s => s.Category == category))))
                    .Where(part => part.Time > TimeSpan.Zero)
                    .ToList();
                return new AppUsage(g.Key, split.MaxBy(part => part.Time).Category, Sum(g), split);
            })
            .OrderByDescending(a => a.Time)
            .ToList();

    private static TimeSpan Sum(IEnumerable<UsageSession> sessions) =>
        sessions.Aggregate(TimeSpan.Zero, (total, s) => total + s.Duration);
}
