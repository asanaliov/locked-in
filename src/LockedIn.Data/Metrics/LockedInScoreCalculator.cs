namespace LockedIn.Data.Metrics;

/// <summary>
/// Locked-In Score (0-100) = 60% focus ratio + 30% longest streak (capped at 90 min) + 10% low switching.
/// </summary>
public static class LockedInScoreCalculator
{
    public const double FocusRatioWeight = 0.6;
    public const double StreakWeight = 0.3;
    public const double SwitchingWeight = 0.1;

    public static readonly TimeSpan StreakCap = TimeSpan.FromMinutes(90);

    /// <summary>At or above this many switches per hour the switching part scores zero.</summary>
    public const double MaxSwitchesPerHour = 60;

    public static int Calculate(double focusRatio, TimeSpan longestStreak, double switchesPerHour)
    {
        var focusPart = Math.Clamp(focusRatio, 0, 1);
        var streakPart = Math.Clamp(longestStreak / StreakCap, 0, 1);
        var switchingPart = 1 - Math.Clamp(switchesPerHour / MaxSwitchesPerHour, 0, 1);

        var score = FocusRatioWeight * focusPart + StreakWeight * streakPart + SwitchingWeight * switchingPart;
        return (int)Math.Round(score * 100, MidpointRounding.AwayFromZero);
    }
}
