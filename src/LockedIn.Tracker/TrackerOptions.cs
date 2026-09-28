namespace LockedIn.Tracker;

public sealed class TrackerOptions
{
    public const string SectionName = "LockedIn:Tracker";

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(3);
    public TimeSpan IdleThreshold { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>When false, window titles are only used for classification and never saved.</summary>
    public bool StoreWindowTitles { get; set; }
}
