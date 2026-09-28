using LockedIn.Data.Metrics;
using LockedIn.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LockedIn.Web.Controllers;

public sealed record TopAppsViewModel(int Days, DayMetrics Metrics);

public sealed class AppsController(DashboardService dashboard) : Controller
{
    private static readonly int[] AllowedDays = [1, 7, 30];

    public async Task<IActionResult> Index(int days = 7, CancellationToken cancellationToken = default)
    {
        if (!AllowedDays.Contains(days))
            days = 7;

        var today = DashboardService.Today;
        var metrics = await dashboard.GetRangeAsync(today.AddDays(1 - days), today, cancellationToken);
        return View(new TopAppsViewModel(days, metrics));
    }
}
