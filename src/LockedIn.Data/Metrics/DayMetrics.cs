namespace LockedIn.Data.Metrics;

public readonly record struct CategoryTime(Category Category, TimeSpan Time);

/// <param name="Category">Where most of the app's time went.</param>
/// <param name="Split">Time per category, Focus first; more than one entry for a browser used for work and play.</param>
public sealed record AppUsage(string AppName, Category Category, TimeSpan Time, IReadOnlyList<CategoryTime> Split);

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
