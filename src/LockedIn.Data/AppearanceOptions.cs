namespace LockedIn.Data;

public enum Theme
{
    System,
    Light,
    Dark,
}

public enum Accent
{
    Blue,
    Graphite,
    Orange,
}

/// <summary>How the dashboard and window look. Shared by the dashboard and the app window's title bar.</summary>
public sealed class AppearanceOptions
{
    public const string SectionName = "LockedIn:Appearance";

    public Theme Theme { get; set; }
    public Accent Accent { get; set; }
    public bool Animations { get; set; } = true;
}
