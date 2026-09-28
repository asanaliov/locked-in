using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace LockedIn.Data;

/// <summary>lockedin.json holds the settings shared by the tracker and the dashboard.</summary>
public static class SettingsFile
{
    public const string SectionName = "LockedIn";

    /// <summary>
    /// Adds lockedin.json as the lowest-priority source, so appsettings.json,
    /// environment variables (e.g. LockedIn__DatabasePath) and the command line can override it.
    /// </summary>
    public static IConfigurationBuilder AddLockedInSettings(this IConfigurationBuilder configuration)
    {
        var source = new JsonConfigurationSource { Path = Path.Combine(AppContext.BaseDirectory, "lockedin.json") };
        source.ResolveFileProvider();
        configuration.Sources.Insert(0, source);
        return configuration;
    }
}
