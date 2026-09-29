namespace LockedIn.Data;

/// <summary>Short human-readable text for durations and ratios, shared by the dashboard and tray.</summary>
public static class Format
{
    public static string Duration(TimeSpan time) => time.TotalHours >= 1
        ? $"{(int)time.TotalHours}h {time.Minutes:00}m"
        : $"{time.Minutes}m";

    /// <summary>To the second, for hover details: "1h 04m 12s", "4m 12s" or "12s".</summary>
    public static string Exact(TimeSpan time)
    {
        if (time.TotalHours >= 1)
            return $"{(int)time.TotalHours}h {time.Minutes:00}m {time.Seconds:00}s";
        return time.TotalMinutes >= 1 ? $"{time.Minutes}m {time.Seconds:00}s" : $"{time.Seconds}s";
    }

    public static string Percent(double ratio) => $"{ratio:P0}";
}
