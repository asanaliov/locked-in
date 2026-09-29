using LockedIn.Data;
using LockedIn.Data.Classification;
using Microsoft.EntityFrameworkCore;

namespace LockedIn.Tracker.Sessions;

/// <summary>Settings page overrides, read from the database once and again only after they change.</summary>
public sealed class DbCategoryOverrides(IDbContextFactory<LockedInDbContext> dbFactory) : ICategoryOverrides
{
    private volatile Dictionary<string, Category>? _overrides;

    public Category? Find(string appName)
    {
        var overrides = _overrides ??= Load();
        return overrides.TryGetValue(appName, out var category) ? category : null;
    }

    public void Invalidate() => _overrides = null;

    private Dictionary<string, Category> Load()
    {
        using var db = dbFactory.CreateDbContext();
        return db.CategoryOverrides.AsNoTracking()
            .ToDictionary(o => o.AppName, o => o.Category, StringComparer.OrdinalIgnoreCase);
    }
}
