using LockedIn.Data.Metrics;

namespace LockedIn.Tests;

public sealed class LockInStagesTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(24, 1)]
    [InlineData(25, 2)]
    [InlineData(49, 2)]
    [InlineData(50, 3)]
    [InlineData(74, 3)]
    [InlineData(75, 4)]
    [InlineData(100, 4)]
    public void Score_maps_to_stage(int score, int expectedLevel) =>
        Assert.Equal(expectedLevel, LockInStages.For(score).Level);

    [Fact]
    public void Locked_in_is_the_top_stage() =>
        Assert.Equal("Locked in", LockInStages.For(100).Name);

    [Fact]
    public void Stages_cover_0_to_100_without_gaps()
    {
        var expectedMin = 0;
        foreach (var stage in LockInStages.All)
        {
            Assert.Equal(expectedMin, stage.MinScore);
            expectedMin = LockInStages.MaxScore(stage) + 1;
        }
        Assert.Equal(101, expectedMin);
    }
}
