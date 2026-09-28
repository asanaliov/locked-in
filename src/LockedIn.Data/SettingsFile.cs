using Microsoft.Extensions.Configuration;

namespace LockedIn.Data;

/// <summary>lockedin.json holds the settings shared by the tracker and the dashboard.</summary>
public static class SettingsFile
{
    public const string SectionName = "LockedIn";

    public static IConfigurationBuilder AddLockedInSettings(this IConfigurationBuilder configuration) =>
        configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "lockedin.json"), optional: false);
}
