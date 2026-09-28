using LockedIn.Data;
using LockedIn.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LockedIn.Web.Controllers;

public sealed class SettingsController(CategorySettingsService settings) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await settings.GetAppsAsync(cancellationToken));

    /// <param name="category">The new category, or null to go back to the lockedin.json default.</param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCategory(string appName, Category? category, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(appName))
            await settings.SetCategoryAsync(appName, category, cancellationToken);

        return RedirectToAction(nameof(Index));
    }
}
