namespace LockedIn.Linux;

/// <summary>"Start at login" through the XDG autostart folder every desktop environment reads. No admin rights needed.</summary>
internal static class Autostart
{
    /// <summary>Passed at login so Locked In starts quietly without opening its window.</summary>
    public const string StartInTrayArgument = "--tray";

    private static string FilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), "autostart", "locked-in.desktop");

    public static bool IsEnabled() => File.Exists(FilePath);

    public static void SetEnabled(bool enabled)
    {
        if (!enabled)
        {
            File.Delete(FilePath);
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, $"""
            [Desktop Entry]
            Type=Application
            Name=Locked In
            Comment=Tracks your screen time and focus
            Exec="{Environment.ProcessPath}" {StartInTrayArgument}
            Icon=locked-in
            Terminal=false

            """);
    }
}
