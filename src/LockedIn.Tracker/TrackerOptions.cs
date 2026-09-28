namespace LockedIn.Tracker;

public sealed class TrackerOptions
{
    public const string SectionName = "LockedIn:Tracker";

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(3);
    public TimeSpan IdleThreshold { get; set; } = TimeSpan.FromMinutes(2);
}
