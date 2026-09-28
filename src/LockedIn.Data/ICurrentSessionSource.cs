namespace LockedIn.Data;

/// <summary>Gives read access to the session the tracker is still building in memory.</summary>
public interface ICurrentSessionSource
{
    /// <summary>A copy of the in-progress session ending now, or null when nothing is being tracked.</summary>
    UsageSession? SnapshotCurrent();
}
