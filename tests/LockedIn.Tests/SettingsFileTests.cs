using LockedIn.Data;

namespace LockedIn.Tests;

public sealed class SettingsFileTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("lockedin-tests-").FullName;

    private string SettingsPath => Path.Combine(_folder, "settings.json");

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Theory]
    [InlineData("")]
    [InlineData("""{ "LockedIn": { "Appear""")]
    public void Unreadable_settings_are_moved_aside_so_the_app_can_start(string content)
    {
        File.WriteAllText(SettingsPath, content);

        SettingsFile.SetAsideIfCorrupt(SettingsPath);

        Assert.False(File.Exists(SettingsPath));
        Assert.Equal(content, File.ReadAllText(SettingsPath + ".bad"));
    }

    [Fact]
    public void Valid_settings_stay_where_they_are()
    {
        File.WriteAllText(SettingsPath, """{ "LockedIn": {} }""");

        SettingsFile.SetAsideIfCorrupt(SettingsPath);

        Assert.True(File.Exists(SettingsPath));
    }

    [Fact]
    public void Missing_settings_are_fine() => SettingsFile.SetAsideIfCorrupt(SettingsPath);

    [Fact]
    public void Saving_replaces_the_whole_file_and_leaves_nothing_else_behind()
    {
        File.WriteAllText(SettingsPath, "old");

        SettingsFile.WriteAtomically(SettingsPath, "new");

        Assert.Equal("new", File.ReadAllText(SettingsPath));
        Assert.Single(Directory.GetFiles(_folder));
    }
}
