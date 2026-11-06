namespace LockedIn.Tracker.Linux;

public static class LinuxSession
{
    /// <summary>
    /// Wayland doesn't let an app see which window has focus, so native Wayland apps can't be tracked
    /// and the window says so instead of quietly showing less time than was really spent.
    /// </summary>
    public static bool IsWayland =>
        string.Equals(Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"), "wayland", StringComparison.OrdinalIgnoreCase)
        || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));
}
