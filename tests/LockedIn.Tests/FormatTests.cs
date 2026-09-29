using LockedIn.Data;

namespace LockedIn.Tests;

public sealed class FormatTests
{
    [Theory]
    [InlineData(0, 0, 12, "12s")]
    [InlineData(0, 4, 5, "4m 05s")]
    [InlineData(1, 4, 12, "1h 04m 12s")]
    [InlineData(13, 0, 0, "13h 00m 00s")]
    public void Exact_shows_seconds(int hours, int minutes, int seconds, string expected) =>
        Assert.Equal(expected, Format.Exact(new TimeSpan(hours, minutes, seconds)));

    [Fact]
    public void Exact_counts_hours_past_a_day() =>
        Assert.Equal("26h 30m 00s", Format.Exact(new TimeSpan(1, 2, 30, 0)));
}
