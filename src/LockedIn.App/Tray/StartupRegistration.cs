using Microsoft.Win32;

namespace LockedIn.App.Tray;

/// <summary>"Start with Windows" via the current user's Run key. No admin rights needed.</summary>
public static class StartupRegistration
{
    /// <summary>Passed at login so Locked In starts quietly in the tray without opening its window.</summary>
    public const string StartInTrayArgument = "--tray";

    // The installer writes the same key and value; keep them in sync with installer/LockedIn.iss.
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "LockedIn";

    private static string Command => $"\"{Environment.ProcessPath}\" {StartInTrayArgument}";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return string.Equals(key?.GetValue(ValueName) as string, Command, StringComparison.OrdinalIgnoreCase);
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled)
            key.SetValue(ValueName, Command);
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
