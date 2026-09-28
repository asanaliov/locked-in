using System.Drawing.Imaging;
using LockedIn.Data;
using LockedIn.Tracker.Windows;

namespace LockedIn.App;

/// <summary>Saves each app's icon from its .exe the first time it's seen. Only the tracker's tick calls it, so no locking.</summary>
internal sealed class AppIconCache(ILogger<AppIconCache> logger) : IAppIconCache
{
    private const int IconSize = 64;

    private readonly HashSet<string> _checked = new(StringComparer.OrdinalIgnoreCase);

    public void Remember(string appName, string executablePath)
    {
        if (!_checked.Add(appName) || AppIconFiles.Exists(appName))
            return;

        try
        {
            using var icon = Icon.ExtractIcon(executablePath, 0, IconSize) ?? Icon.ExtractAssociatedIcon(executablePath);
            if (icon is null)
                return;

            Directory.CreateDirectory(AppIconFiles.Folder);
            using var bitmap = icon.ToBitmap();
            bitmap.Save(AppIconFiles.PathFor(appName), ImageFormat.Png);
        }
        catch (Exception ex)
        {
            // An icon is only decoration; the dashboard falls back to the app's initial.
            logger.LogDebug(ex, "Could not save the icon of {App}", appName);
        }
    }
}
