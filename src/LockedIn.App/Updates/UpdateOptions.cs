namespace LockedIn.App.Updates;

public sealed class UpdateOptions
{
    public const string SectionName = "LockedIn:Updates";

    /// <summary>Ask GitHub about a new release shortly after start and then once a day.</summary>
    public bool CheckAutomatically { get; set; } = true;
}
