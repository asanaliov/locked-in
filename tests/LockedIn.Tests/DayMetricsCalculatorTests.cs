using LockedIn.Data.Metrics;
using static LockedIn.Data.Category;
using static LockedIn.Tests.Sessions;

namespace LockedIn.Tests;

public sealed class DayMetricsCalculatorTests
{
    private static readonly TimeSpan Tolerance = TimeSpan.FromSeconds(30);

    [Fact]
    public void Empty_day_has_zero_score()
    {
        var metrics = DayMetricsCalculator.Calculate([], Tolerance);

        Assert.Equal(0, metrics.Score);
        Assert.Equal(TimeSpan.Zero, metrics.ActiveTime);
        Assert.Equal(0, metrics.FocusRatio);
    }

    [Fact]
    public void Sums_time_per_category_and_app()
    {
        var metrics = DayMetricsCalculator.Calculate(
        [
            At(0, 30, Focus, "rider64"),
            At(30, 40, Distraction, "Discord"),
            At(40, 50, Neutral, "explorer"),
            At(50, 60, Focus, "rider64"),
        ], Tolerance);

        Assert.Equal(TimeSpan.FromMinutes(60), metrics.ActiveTime);
        Assert.Equal(TimeSpan.FromMinutes(40), metrics.FocusTime);
        Assert.Equal(TimeSpan.FromMinutes(10), metrics.DistractionTime);
        Assert.Equal(TimeSpan.FromMinutes(10), metrics.NeutralTime);
        Assert.Equal("rider64", metrics.Apps[0].AppName);
        Assert.Equal(TimeSpan.FromMinutes(40), metrics.Apps[0].Time);
        Assert.Equal(3, metrics.SwitchesPerHour);
    }

    [Fact]
    public void Category_split_within_one_app_is_not_a_context_switch()
    {
        var sessions = new[] { At(0, 10, Focus, "chrome"), At(10, 20, Distraction, "chrome") };

        Assert.Equal(0, DayMetricsCalculator.CountContextSwitches(sessions));
    }
}
