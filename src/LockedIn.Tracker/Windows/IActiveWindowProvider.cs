namespace LockedIn.Tracker.Windows;

public sealed record ActiveWindow(string AppName, string Title);

public interface IActiveWindowProvider
{
    /// <summary>Returns the foreground window, or null when there is none (e.g. lock screen).</summary>
    ActiveWindow? GetActiveWindow();
}
