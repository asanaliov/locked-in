using LockedIn.App.Updates;

namespace LockedIn.Tests;

public sealed class ReleaseFeedTests
{
    private static readonly Version Current = new(0, 3, 0, 0);

    private static string Release(string tag, string assetName = "locked-in-setup-{0}.exe", string? url = null)
    {
        var version = tag.TrimStart('v');
        var name = string.Format(assetName, version);
        url ??= $"https://github.com/asanaliov/locked-in/releases/download/{tag}/{name}";
        return $$"""
            {
              "tag_name": "{{tag}}",
              "html_url": "https://github.com/asanaliov/locked-in/releases/tag/{{tag}}",
              "assets": [
                { "name": "notes.txt", "browser_download_url": "https://github.com/asanaliov/locked-in/releases/download/{{tag}}/notes.txt" },
                { "name": "{{name}}", "browser_download_url": "{{url}}" }
              ]
            }
            """;
    }

    [Fact]
    public void Newer_release_offers_its_installer()
    {
        var update = ReleaseFeed.Parse(Release("v0.4.0"), Current);

        Assert.NotNull(update);
        Assert.Equal(new Version(0, 4, 0), update.Version);
        Assert.EndsWith("/v0.4.0/locked-in-setup-0.4.0.exe", update.InstallerUrl.AbsoluteUri);
    }

    [Theory]
    [InlineData("v0.3.0")]
    [InlineData("v0.3")]
    [InlineData("v0.2.9")]
    public void Same_or_older_release_is_not_an_update(string tag) =>
        Assert.Null(ReleaseFeed.Parse(Release(tag), Current));

    [Fact]
    public void Installer_hosted_anywhere_else_is_ignored() =>
        Assert.Null(ReleaseFeed.Parse(Release("v0.4.0", url: "https://example.com/locked-in-setup-0.4.0.exe"), Current));

    [Fact]
    public void Release_without_an_installer_is_ignored() =>
        Assert.Null(ReleaseFeed.Parse(Release("v0.4.0", assetName: "LockedIn-{0}.zip"), Current));

    [Fact]
    public void Tag_that_is_not_a_version_is_ignored() =>
        Assert.Null(ReleaseFeed.Parse(Release("nightly"), Current));
}
