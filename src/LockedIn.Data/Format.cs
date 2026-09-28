namespace LockedIn.Data;

/// <summary>Short human-readable text for durations and ratios, shared by the dashboard and tray.</summary>
public static class Format
{
    public static string Duration(TimeSpan time) => time.TotalHours >= 1
        ? $"{(int)time.TotalHours}h {time.Minutes:00}m"
        : $"{time.Minutes}m";

    public static string Percent(double ratio) => $"{ratio:P0}";
}
