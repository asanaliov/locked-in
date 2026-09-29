using System.Windows.Media;
using LockedIn.App.Controls;
using LockedIn.Data;
using LockedIn.Data.Metrics;

namespace LockedIn.App.Views;

/// <summary>One line of an app list, shown with the shared "AppRow" template.</summary>
public sealed record AppRow(string AppName, Category Category, string Time, string? Sub, string? Detail, IReadOnlyList<BarSegment> Bar, double Total)
{
    public bool HasSub => Sub is not null;
    public bool HasDetail => Detail is not null;

    /// <param name="barBrush">Bar colour per app.</param>
    /// <param name="withCategoryAndShare">Adds the category under the name and the share of active time under the time.</param>
    public static IReadOnlyList<AppRow> From(IReadOnlyList<AppUsage> apps, TimeSpan activeTime, Func<AppUsage, Brush> barBrush, bool withCategoryAndShare = false)
    {
        var longest = apps.Count > 0 ? apps[0].Time.TotalMinutes : 0;
        return apps.Select(app => new AppRow(
                app.AppName,
                app.Category,
                Format.Duration(app.Time),
                withCategoryAndShare ? app.Category.ToString() : null,
                withCategoryAndShare && activeTime > TimeSpan.Zero ? Format.Percent(app.Time / activeTime) : null,
                [new BarSegment(app.Time.TotalMinutes, barBrush(app))],
                longest))
            .ToList();
    }
}
