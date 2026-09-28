using LockedIn.Data.Metrics;
using static LockedIn.Data.Category;
using static LockedIn.Tests.Sessions;

namespace LockedIn.Tests;

public sealed class StreakCalculatorTests
{
    private static readonly TimeSpan Tolerance = TimeSpan.FromSeconds(30);

    [Fact]
    public void No_focus_means_no_streak()
    {
        var sessions = new[] { At(0, 30, Neutral), At(30, 60, Distraction) };

        Assert.Equal(TimeSpan.Zero, StreakCalculator.LongestFocusStreak(sessions, Tolerance));
    }

    [Fact]
    public void Short_interruption_does_not_break_the_streak()
    {
        var sessions = new[] { At(0, 20, Focus), At(20, 20.25, Distraction), At(20.25, 50, Focus) };

        Assert.Equal(TimeSpan.FromMinutes(50), StreakCalculator.LongestFocusStreak(sessions, Tolerance));
    }

    [Fact]
    public void Long_interruption_breaks_the_streak()
    {
        var sessions = new[] { At(0, 20, Focus), At(20, 25, Distraction), At(25, 40, Focus) };

        Assert.Equal(TimeSpan.FromMinutes(20), StreakCalculator.LongestFocusStreak(sessions, Tolerance));
    }

    [Fact]
    public void Idle_gap_counts_as_an_interruption()
    {
        var sessions = new[] { At(0, 10, Focus), At(15, 45, Focus) };

        Assert.Equal(TimeSpan.FromMinutes(30), StreakCalculator.LongestFocusStreak(sessions, Tolerance));
    }

    [Fact]
    public void Switching_between_focus_apps_keeps_the_streak()
    {
        var sessions = new[] { At(0, 10, Focus, "rider64"), At(10, 25, Focus, "WindowsTerminal") };

        Assert.Equal(TimeSpan.FromMinutes(25), StreakCalculator.LongestFocusStreak(sessions, Tolerance));
    }

    [Fact]
    public void Order_of_input_does_not_matter()
    {
        var sessions = new[] { At(20.25, 50, Focus), At(0, 20, Focus) };

        Assert.Equal(TimeSpan.FromMinutes(50), StreakCalculator.LongestFocusStreak(sessions, Tolerance));
    }
}
