using LockedIn.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LockedIn.Web.Controllers;

public sealed record ScreenTimeViewModel(IReadOnlyList<DaySummary> Week, double[] MinutesPerHour)
{
    public DaySummary Today => Week[^1];

    /// <summary>Average over the days that have any tracked time, so a fresh install isn't dragged down by empty days.</summary>
    public TimeSpan DailyAverage
    {
        get
        {
            var tracked = Week.Where(d => d.Metrics.ActiveTime > TimeSpan.Zero).ToList();
            return tracked.Count == 0 ? TimeSpan.Zero : tracked.Aggregate(TimeSpan.Zero, (sum, d) => sum + d.Metrics.ActiveTime) / tracked.Count;
        }
    }
}

public sealed class ScreenTimeController(DashboardService dashboard) : Controller
{
    private const int Days = 7;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var week = await dashboard.GetLastDaysAsync(Days, cancellationToken);
        var hours = await dashboard.GetMinutesPerHourAsync(DashboardService.Today, cancellationToken);
        return View(new ScreenTimeViewModel(week, hours));
    }
}
