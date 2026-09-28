namespace LockedIn.Data;

/// <summary>A continuous stretch of time spent in one app with one category. Times are UTC.</summary>
public sealed class UsageSession
{
    public int Id { get; set; }
    public required string AppName { get; set; }
    public Category Category { get; set; }
    public string? WindowTitle { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public TimeSpan Duration => EndTime - StartTime;
}
