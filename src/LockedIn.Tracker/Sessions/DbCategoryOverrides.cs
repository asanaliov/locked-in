using LockedIn.Data;
using LockedIn.Data.Classification;
using Microsoft.EntityFrameworkCore;

namespace LockedIn.Tracker.Sessions;

/// <summary>Settings page overrides, re-read from the database at most once a minute.</summary>
public sealed class DbCategoryOverrides(IDbContextFactory<LockedInDbContext> dbFactory, IClock clock) : ICategoryOverrides
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(1);

    private Dictionary<string, Category> _overrides = new(StringComparer.OrdinalIgnoreCase);
    private DateTime _loadedAt = DateTime.MinValue;

    public Category? Find(string appName)
    {
        if (clock.UtcNow - _loadedAt > RefreshInterval)
            Reload();

        return _overrides.TryGetValue(appName, out var category) ? category : null;
    }

    private void Reload()
    {
        using var db = dbFactory.CreateDbContext();
        _overrides = db.CategoryOverrides.AsNoTracking()
            .ToDictionary(o => o.AppName, o => o.Category, StringComparer.OrdinalIgnoreCase);
        _loadedAt = clock.UtcNow;
    }
}
