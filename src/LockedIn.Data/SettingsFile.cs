using System.Text.Json;
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
    /// A settings file that can't be read would stop the app, and so the tracking, from starting at all.
    /// It's kept as settings.json.bad and the defaults apply until the user changes something.
    /// </summary>
    public static void SetAsideIfCorrupt(string path)
    {
        if (!File.Exists(path))
            return;

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
        }
        catch (JsonException)
        {
            File.Move(path, path + ".bad", overwrite: true);
        }
    }

    /// <summary>Writes to a temporary file first, so a crash or power cut can't leave a half-written file behind.</summary>
    public static void WriteAtomically(string path, string content)
    {
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, content);
        File.Move(temporary, path, overwrite: true);
    }

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
        SetAsideIfCorrupt(UserSettingsPath);
        var user = new JsonConfigurationSource { Path = UserSettingsPath, Optional = true };
        user.ResolveFileProvider();
        configuration.Sources.Insert(1, user);
        return configuration;
    }
}
