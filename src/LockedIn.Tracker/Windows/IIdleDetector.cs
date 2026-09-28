namespace LockedIn.Tracker.Windows;

public interface IIdleDetector
{
    /// <summary>Time since the last keyboard or mouse input.</summary>
    TimeSpan GetIdleTime();
}
