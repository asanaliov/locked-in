using System.Text.Json;

namespace LockedIn.App.Updates;

public sealed record AvailableUpdate(Version Version, Uri InstallerUrl);

/// <summary>Reads GitHub's "latest release" answer for this repository.</summary>
public static class ReleaseFeed
{
    public const string LatestReleaseUrl = "https://api.github.com/repos/asanaliov/locked-in/releases/latest";

    // Only installers published on this repository's own releases are ever downloaded.
    private const string TrustedDownloadPrefix = "https://github.com/asanaliov/locked-in/releases/download/";

    /// <summary>The newer release and its installer, or null when there is nothing newer to install.</summary>
    public static AvailableUpdate? Parse(string json, Version current)
    {
        using var document = JsonDocument.Parse(json);
        var release = document.RootElement;

        if (!release.TryGetProperty("tag_name", out var tag) || !Version.TryParse(tag.GetString()?.TrimStart('v'), out var version))
            return null;
        if (Comparable(version) <= Comparable(current))
            return null;
        if (!release.TryGetProperty("assets", out var assets))
            return null;

        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? "";
            var url = asset.GetProperty("browser_download_url").GetString() ?? "";
            if (name.StartsWith("locked-in-setup-", StringComparison.OrdinalIgnoreCase)
                && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                && url.StartsWith(TrustedDownloadPrefix, StringComparison.Ordinal))
            {
                return new AvailableUpdate(version, new Uri(url));
            }
        }
        return null;
    }

    /// <summary>"0.3" and "0.3.0.0" are the same release.</summary>
    private static Version Comparable(Version version) => new(version.Major, version.Minor, Math.Max(version.Build, 0));
}
