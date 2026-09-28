namespace LockedIn.App;

internal static class AppIcon
{
    private static readonly Lazy<Icon> Icon = new(() =>
        System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? SystemIcons.Application);

    /// <summary>The icon embedded in LockedIn.exe, shared by the tray and the window.</summary>
    public static Icon Value => Icon.Value;
}
