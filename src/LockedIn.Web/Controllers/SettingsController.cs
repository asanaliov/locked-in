using LockedIn.Data;
using LockedIn.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LockedIn.Web.Controllers;

public sealed record SettingsViewModel(GeneralSettings General, IReadOnlyList<AppCategoryRow> Apps);

public sealed class SettingsController(CategorySettingsService settings, GeneralSettingsService general) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(new SettingsViewModel(general.Get(), await settings.GetAppsAsync(cancellationToken)));

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SaveGeneral(GeneralSettings model)
    {
        general.Save(model);
        return RedirectToAction(nameof(Index));
    }

    /// <param name="category">The new category, or null to go back to the lockedin.json default.</param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCategory(string appName, Category? category, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(appName))
            await settings.SetCategoryAsync(appName, category, cancellationToken);

        return RedirectToAction(nameof(Index), null, "categories");
    }
}
