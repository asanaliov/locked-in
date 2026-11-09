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

    [Fact]
    public void Clocks_going_back_does_not_lose_time()
    {
        // New York, 1 Nov 2026: 01:30 EDT to 01:30 EST is a real hour with the same local time at both ends.
        var session = Utc(new DateTime(2026, 11, 1, 5, 30, 0), new DateTime(2026, 11, 1, 6, 30, 0));

        var minutes = HourlyUsageCalculator.MinutesPerHour([session], NewYork);

        Assert.Equal(60, minutes[1], precision: 6);
        Assert.Equal(60, minutes.Sum(), precision: 6);
    }

    [Fact]
    public void Clocks_going_forward_does_not_add_time()
    {
        // New York, 8 Mar 2026: 01:50 EST to 03:10 EDT is 20 real minutes; 02:00-03:00 never happened.
        var session = Utc(new DateTime(2026, 3, 8, 6, 50, 0), new DateTime(2026, 3, 8, 7, 10, 0));

        var minutes = HourlyUsageCalculator.MinutesPerHour([session], NewYork);

        Assert.Equal(10, minutes[1], precision: 6);
        Assert.Equal(0, minutes[2], precision: 6);
        Assert.Equal(10, minutes[3], precision: 6);
    }

    private static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    private static UsageSession Utc(DateTime start, DateTime end) => new()
    {
        AppName = "rider64",
        Category = Category.Focus,
        StartTime = DateTime.SpecifyKind(start, DateTimeKind.Utc),
        EndTime = DateTime.SpecifyKind(end, DateTimeKind.Utc),
    };

    /// <summary>A session on a fixed day, given in local time and stored as UTC like the tracker does.</summary>
    private static UsageSession Local(int startHour, int startMinute, int endHour, int endMinute) => new()
    {
        AppName = "rider64",
        Category = Category.Focus,
        StartTime = new DateTime(2026, 3, 10, startHour, startMinute, 0, DateTimeKind.Local).ToUniversalTime(),
        EndTime = new DateTime(2026, 3, 10, endHour, endMinute, 0, DateTimeKind.Local).ToUniversalTime(),
    };
}
