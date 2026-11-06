using System.Text.Json;

namespace LockedIn.Core.Updates;

public sealed record AvailableUpdate(Version Version, Uri InstallerUrl);

/// <summary>Which file of a release this copy of the app can install.</summary>
public enum ReleasePlatform
{
    Windows,
    Linux,
}

/// <summary>Reads GitHub's "latest release" answer for this repository.</summary>
public static class ReleaseFeed
{
    public const string LatestReleaseUrl = "https://api.github.com/repos/asanaliov/locked-in/releases/latest";

    /// <summary>Where Linux users get a new version: their package manager or tarball is theirs to update.</summary>
    public const string LatestReleasePage = "https://github.com/asanaliov/locked-in/releases/latest";

    // Only installers published on this repository's own releases are ever downloaded.
    private const string TrustedDownloadPrefix = "https://github.com/asanaliov/locked-in/releases/download/";

    /// <summary>The newer release and its installer, or null when there is nothing newer to install.</summary>
    /// <param name="platform">The platform this process runs on by default.</param>
    public static AvailableUpdate? Parse(string json, Version current, ReleasePlatform? platform = null)
    {
        using var document = JsonDocument.Parse(json);
        try
        {
            return Read(document.RootElement, current, platform ?? (OperatingSystem.IsWindows() ? ReleasePlatform.Windows : ReleasePlatform.Linux));
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or UriFormatException)
        {
            return null; // valid JSON, but not the shape of a release
        }
    }

    private static bool IsInstaller(string name, ReleasePlatform platform) => platform == ReleasePlatform.Windows
        ? name.StartsWith("locked-in-setup-", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
        : name.StartsWith("locked-in-", StringComparison.OrdinalIgnoreCase) && name.EndsWith("-linux-x64.tar.gz", StringComparison.OrdinalIgnoreCase);

    private static AvailableUpdate? Read(JsonElement release, Version current, ReleasePlatform platform)
    {
        if (!release.TryGetProperty("tag_name", out var tag) || !Version.TryParse(tag.GetString()?.TrimStart('v'), out var parsed))
            return null;

        var version = Comparable(parsed);
        if (version <= Comparable(current))
            return null;
        if (!release.TryGetProperty("assets", out var assets))
            return null;

        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? "";
            var url = asset.GetProperty("browser_download_url").GetString() ?? "";
            if (IsInstaller(name, platform) && url.StartsWith(TrustedDownloadPrefix, StringComparison.Ordinal))
            {
                return new AvailableUpdate(version, new Uri(url));
            }
        }
        return null;
    }

    /// <summary>"0.3" and "0.3.0.0" are the same release.</summary>
    private static Version Comparable(Version version) => new(version.Major, version.Minor, Math.Max(version.Build, 0));
}
