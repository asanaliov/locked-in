namespace LockedIn.Data.Metrics;

public sealed record AppUsage(string AppName, Category Category, TimeSpan Time);

public sealed record DayMetrics(
    TimeSpan ActiveTime,
    TimeSpan FocusTime,
    TimeSpan NeutralTime,
    TimeSpan DistractionTime,
    TimeSpan LongestStreak,
    double SwitchesPerHour,
    int Score,
    IReadOnlyList<AppUsage> Apps)
{
    public double FocusRatio => ActiveTime > TimeSpan.Zero ? FocusTime / ActiveTime : 0;
}
