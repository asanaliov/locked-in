namespace LockedIn.Tracker.Windows;

/// <summary>Keeps a copy of each app's icon so the dashboard can show it.</summary>
public interface IAppIconCache
{
    /// <summary>Called when a session starts; cheap to call again for an app it already has.</summary>
    void Remember(string appName, string executablePath);
}
