using LockedIn.Data;
using LockedIn.Data.Classification;
using Microsoft.EntityFrameworkCore;

namespace LockedIn.Web.Services;

public sealed record AppCategoryRow(string AppName, Category DefaultCategory, Category? Override, bool IsBrowser)
{
    public Category Effective => Override ?? DefaultCategory;
}

public sealed class CategorySettingsService(
    IDbContextFactory<LockedInDbContext> dbFactory,
    CategoryRules rules,
    ICategoryOverrides overrides)
{
    /// <summary>Every app that was tracked, has a default rule, or has an override.</summary>
    public async Task<IReadOnlyList<AppCategoryRow>> GetAppsAsync(CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var overrides = await db.CategoryOverrides.AsNoTracking()
            .ToDictionaryAsync(o => o.AppName, o => o.Category, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var tracked = await db.UsageSessions.Select(s => s.AppName).Distinct().ToListAsync(cancellationToken);

        return tracked.Concat(rules.Apps.Keys).Concat(overrides.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(app => new AppCategoryRow(
                app,
                rules.DefaultFor(app),
                overrides.TryGetValue(app, out var category) ? category : null,
                rules.Browsers.Contains(app)))
            .ToList();
    }

    /// <summary>
    /// Saves the override (null resets to the default) and re-categorizes the app's past sessions.
    /// Browser history is left alone because it was classified per tab title.
    /// </summary>
    public async Task SetCategoryAsync(string appName, Category? category, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.CategoryOverrides.FindAsync([appName], cancellationToken);
        switch (existing, category)
        {
            case (null, { } chosen):
                db.CategoryOverrides.Add(new AppCategoryOverride { AppName = appName, Category = chosen });
                break;
            case (not null, { } chosen):
                existing.Category = chosen;
                break;
            case (not null, null):
                db.CategoryOverrides.Remove(existing);
                break;
        }
        await db.SaveChangesAsync(cancellationToken);
        overrides.Invalidate();

        if (!rules.Browsers.Contains(appName))
        {
            var effective = category ?? rules.DefaultFor(appName);
            await db.UsageSessions
                .Where(s => s.AppName == appName)
                .ExecuteUpdateAsync(set => set.SetProperty(s => s.Category, effective), cancellationToken);
        }
    }
}
