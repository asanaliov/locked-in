using LockedIn.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LockedIn.Web.Controllers;

public sealed class TodayController(DashboardService dashboard) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await dashboard.GetDayAsync(DashboardService.Today, cancellationToken));
}
