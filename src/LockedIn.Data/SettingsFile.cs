using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace LockedIn.Data;

/// <summary>
/// lockedin.json holds the default settings shared by the tracker and the dashboard.
/// settings.json holds what the user changed in the dashboard; it lives next to the data so it survives updates.
/// </summary>
public static class SettingsFile
{
    public const string SectionName = "LockedIn";

    public static readonly string UserSettingsPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LockedIn", "settings.json");

    /// <summary>
    /// Adds lockedin.json and then the user's settings.json as the lowest-priority sources, so appsettings.json,
    /// environment variables (e.g. LockedIn__DatabasePath) and the command line can override both.
    /// </summary>
    public static IConfigurationBuilder AddLockedInSettings(this IConfigurationBuilder configuration)
    {
        var defaults = new JsonConfigurationSource { Path = Path.Combine(AppContext.BaseDirectory, "lockedin.json") };
        defaults.ResolveFileProvider();
        configuration.Sources.Insert(0, defaults);

        Directory.CreateDirectory(Path.GetDirectoryName(UserSettingsPath)!);
        var user = new JsonConfigurationSource { Path = UserSettingsPath, Optional = true };
        user.ResolveFileProvider();
        configuration.Sources.Insert(1, user);
        return configuration;
    }
}
