using LockedIn.Data.Metrics;

namespace LockedIn.Tests;

public sealed class LockedInScoreCalculatorTests
{
    [Fact]
    public void Perfect_day_scores_100() =>
        Assert.Equal(100, LockedInScoreCalculator.Calculate(1.0, TimeSpan.FromMinutes(90), 0));

    [Fact]
    public void No_focus_and_constant_switching_scores_0() =>
        Assert.Equal(0, LockedInScoreCalculator.Calculate(0, TimeSpan.Zero, 60));

    [Fact]
    public void Streak_is_capped_at_90_minutes() =>
        Assert.Equal(
            LockedInScoreCalculator.Calculate(0.5, TimeSpan.FromMinutes(90), 10),
            LockedInScoreCalculator.Calculate(0.5, TimeSpan.FromHours(4), 10));

    [Fact]
    public void Components_are_weighted_60_30_10()
    {
        Assert.Equal(60, LockedInScoreCalculator.Calculate(1.0, TimeSpan.Zero, 60));
        Assert.Equal(30, LockedInScoreCalculator.Calculate(0, TimeSpan.FromMinutes(90), 60));
        Assert.Equal(10, LockedInScoreCalculator.Calculate(0, TimeSpan.Zero, 0));
    }

    [Fact]
    public void Mixed_day_is_scored_proportionally()
    {
        // 0.6 * 0.5 + 0.3 * (45 / 90) + 0.1 * (1 - 30 / 60) = 0.30 + 0.15 + 0.05
        Assert.Equal(50, LockedInScoreCalculator.Calculate(0.5, TimeSpan.FromMinutes(45), 30));
    }
}
