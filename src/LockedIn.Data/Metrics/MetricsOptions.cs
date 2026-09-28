namespace LockedIn.Data.Metrics;

public sealed class MetricsOptions
{
    public const string SectionName = "LockedIn:Metrics";

    /// <summary>Non-focus gaps shorter than this don't break a deep work streak.</summary>
    public TimeSpan StreakInterruptionTolerance { get; set; } = TimeSpan.FromSeconds(30);
}
