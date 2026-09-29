using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using LockedIn.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace LockedIn.App.Services;

public sealed record GeneralSettings(
    Theme Theme,
    Accent Accent,
    bool Animations,
    int IdleMinutes,
    bool StoreWindowTitles,
    int StreakToleranceSeconds)
{
    public static readonly int[] IdleMinuteChoices = [1, 2, 5, 10, 15];
    public static readonly int[] StreakToleranceChoices = [15, 30, 60, 120, 300];
}

/// <summary>Reads the effective settings and saves changes to the user's settings.json.</summary>
public sealed class GeneralSettingsService(IConfiguration configuration, IOptionsMonitor<AppearanceOptions> appearance)
{
    private const string TrackerSection = $"{SettingsFile.SectionName}:Tracker";
    private const string MetricsSection = $"{SettingsFile.SectionName}:Metrics";

    public GeneralSettings Get()
    {
        var look = appearance.CurrentValue;
        return new GeneralSettings(
            look.Theme,
            look.Accent,
            look.Animations,
            (int)configuration.GetValue($"{TrackerSection}:IdleThreshold", TimeSpan.FromMinutes(2)).TotalMinutes,
            configuration.GetValue<bool>($"{TrackerSection}:StoreWindowTitles"),
            (int)configuration.GetValue($"{MetricsSection}:StreakInterruptionTolerance", TimeSpan.FromSeconds(30)).TotalSeconds);
    }

    public void Save(GeneralSettings settings)
    {
        var idle = GeneralSettings.IdleMinuteChoices.Contains(settings.IdleMinutes) ? settings.IdleMinutes : 2;
        var tolerance = GeneralSettings.StreakToleranceChoices.Contains(settings.StreakToleranceSeconds) ? settings.StreakToleranceSeconds : 30;

        var json = new JsonObject
        {
            [SettingsFile.SectionName] = new JsonObject
            {
                ["Appearance"] = new JsonObject
                {
                    ["Theme"] = (Enum.IsDefined(settings.Theme) ? settings.Theme : Theme.System).ToString(),
                    ["Accent"] = (Enum.IsDefined(settings.Accent) ? settings.Accent : Accent.Blue).ToString(),
                    ["Animations"] = settings.Animations,
                },
                ["Tracker"] = new JsonObject
                {
                    ["IdleThreshold"] = TimeSpan.FromMinutes(idle).ToString(),
                    ["StoreWindowTitles"] = settings.StoreWindowTitles,
                },
                ["Metrics"] = new JsonObject
                {
                    ["StreakInterruptionTolerance"] = TimeSpan.FromSeconds(tolerance).ToString(),
                },
            },
        };
        File.WriteAllText(SettingsFile.UserSettingsPath, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        // Reload right away instead of waiting on a file watcher, so the redirect already shows the new values.
        ((IConfigurationRoot)configuration).Reload();
    }
}
