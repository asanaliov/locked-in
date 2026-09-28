namespace LockedIn.Web;

public static class Format
{
    public static string Duration(TimeSpan time) => time.TotalHours >= 1
        ? $"{(int)time.TotalHours}h {time.Minutes:00}m"
        : $"{time.Minutes}m";

    public static string Percent(double ratio) => $"{ratio:P0}";
}
