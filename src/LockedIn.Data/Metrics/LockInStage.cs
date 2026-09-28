namespace LockedIn.Data.Metrics;

/// <summary>A named band of the Locked In Score, shown with its brain image on the dashboard.</summary>
public sealed record LockInStage(int Level, string Name, int MinScore);

public static class LockInStages
{
    public static readonly IReadOnlyList<LockInStage> All =
    [
        new(1, "Brain idle", 0),
        new(2, "Warming up", 25),
        new(3, "Locked in", 50),
        new(4, "Galaxy brain", 75),
    ];

    public static LockInStage For(int score) => All.LastOrDefault(stage => score >= stage.MinScore) ?? All[0];

    /// <summary>The highest score in the stage, e.g. 49 for "Warming up".</summary>
    public static int MaxScore(LockInStage stage) =>
        stage.Level < All.Count ? All[stage.Level].MinScore - 1 : 100;
}
