using LockedIn.Data;
using Microsoft.EntityFrameworkCore;

namespace LockedIn.Tracker.Sessions;

public sealed class EfSessionStore(IDbContextFactory<LockedInDbContext> dbFactory) : ISessionStore
{
    public async Task SaveAsync(UsageSession session, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        db.UsageSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
    }
}
