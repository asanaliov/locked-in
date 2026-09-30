using System.Windows.Media;
using LockedIn.App.Controls;
using LockedIn.App.Theming;
using LockedIn.Data;
using LockedIn.Data.Metrics;

namespace LockedIn.App.Views;

/// <summary>One line of an app list, shown with the shared "AppRow" template.</summary>
/// <param name="ExactTime">Shown on hover, to the second, with the split for apps used in more than one way.</param>
public sealed record AppRow(string AppName, Category Category, string Time, string ExactTime, string? Sub, string? Detail, IReadOnlyList<BarSegment> Bar, double Total)
{
    /// <summary>A category needs at least this share of an app's time before the app counts as mixed.</summary>
    private const double MixedShare = 0.05;

    public bool HasSub => Sub is not null;
    public bool HasDetail => Detail is not null;

    /// <param name="barColour">One colour for every bar; null colours each bar by category, split where an app is mixed.</param>
    /// <param name="withCategoryAndShare">Adds the category under the name and the share of active time under the time.</param>
    public static IReadOnlyList<AppRow> From(IReadOnlyList<AppUsage> apps, TimeSpan activeTime, Brush? barColour = null, bool withCategoryAndShare = false)
    {
        var longest = apps.Count > 0 ? apps[0].Time.TotalMinutes : 0;
        return apps.Select(app => new AppRow(
                app.AppName,
                app.Category,
                Format.Duration(app.Time),
                ExactTimeText(app),
                withCategoryAndShare ? CategoryLabel(app) : null,
                withCategoryAndShare && activeTime > TimeSpan.Zero ? Format.Percent(app.Time / activeTime) : null,
                barColour is null
                    ? app.Split.Select(part => new BarSegment(part.Time.TotalMinutes, ThemeManager.For(part.Category))).ToList()
                    : [new BarSegment(app.Time.TotalMinutes, barColour)],
                longest))
            .ToList();
    }

    private static bool IsMixed(AppUsage app) => app.Split.Count(part => part.Time / app.Time >= MixedShare) > 1;

    private static string CategoryLabel(AppUsage app) => IsMixed(app) ? "Mixed" : app.Category.ToString();

    private static string ExactTimeText(AppUsage app)
    {
        var total = $"{app.AppName} · {Format.Exact(app.Time)}";
        return app.Split.Count > 1
            ? total + "\n" + string.Join(" · ", app.Split.Select(part => $"{part.Category} {Format.Exact(part.Time)}"))
            : total;
    }
}
