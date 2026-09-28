using LockedIn.Data;
using Microsoft.AspNetCore.Mvc;

namespace LockedIn.Web.Controllers;

public sealed class IconsController : Controller
{
    [ResponseCache(Duration = 3600)]
    public IActionResult Get(string? app) =>
        !string.IsNullOrWhiteSpace(app) && AppIconFiles.Exists(app)
            ? PhysicalFile(AppIconFiles.PathFor(app), "image/png")
            : NotFound();
}
