using LockedIn.Data;

namespace LockedIn.Tracker.Sessions;

public interface ISessionStore
{
    Task SaveAsync(UsageSession session, CancellationToken cancellationToken);
}
