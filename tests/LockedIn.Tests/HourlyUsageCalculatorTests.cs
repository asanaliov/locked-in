using LockedIn.Data;
using LockedIn.Data.Metrics;

namespace LockedIn.Tests;

public sealed class HourlyUsageCalculatorTests
{
    [Fact]
    public void Empty_day_is_all_zero()
    {
        var minutes = HourlyUsageCalculator.MinutesPerHour([]);

        Assert.Equal(24, minutes.Length);
        Assert.All(minutes, m => Assert.Equal(0, m));
    }

    [Fact]
    public void Session_inside_one_hour_counts_there()
    {
        var minutes = HourlyUsageCalculator.MinutesPerHour([Local(9, 10, 9, 40)]);

        Assert.Equal(30, minutes[9], precision: 6);
        Assert.Equal(30, minutes.Sum(), precision: 6);
    }

    [Fact]
    public void Session_across_hours_is_split()
    {
        var minutes = HourlyUsageCalculator.MinutesPerHour([Local(9, 45, 11, 15)]);

        Assert.Equal(15, minutes[9], precision: 6);
        Assert.Equal(60, minutes[10], precision: 6);
        Assert.Equal(15, minutes[11], precision: 6);
    }

    /// <summary>A session on a fixed day, given in local time and stored as UTC like the tracker does.</summary>
    private static UsageSession Local(int startHour, int startMinute, int endHour, int endMinute) => new()
    {
        AppName = "rider64",
        Category = Category.Focus,
        StartTime = new DateTime(2026, 3, 10, startHour, startMinute, 0, DateTimeKind.Local).ToUniversalTime(),
        EndTime = new DateTime(2026, 3, 10, endHour, endMinute, 0, DateTimeKind.Local).ToUniversalTime(),
    };
}
