using LockedIn.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LockedIn.Web.Controllers;

public sealed class HistoryController(DashboardService dashboard) : Controller
{
    private const int Days = 7;

    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await dashboard.GetLastDaysAsync(Days, cancellationToken));
}
