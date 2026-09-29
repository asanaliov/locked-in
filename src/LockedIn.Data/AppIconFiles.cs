namespace LockedIn.Data;

/// <summary>App icons cached as PNGs next to the data, one per process name. Written by the app, read by the dashboard.</summary>
public static class AppIconFiles
{
    public static readonly string Folder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LockedIn", "icons");

    public static string PathFor(string appName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safeName = string.Concat(appName.Select(c => invalid.Contains(c) ? '_' : c));
        return Path.Combine(Folder, safeName + ".png");
    }

    public static bool Exists(string appName) => File.Exists(PathFor(appName));
}
