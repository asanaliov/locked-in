using System.Drawing;

namespace LockedIn.App;

internal static class AppIcon
{
    private static readonly Lazy<Icon> Icon = new(() =>
        System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? SystemIcons.Application);

    /// <summary>The icon embedded in LockedIn.exe, for the tray.</summary>
    public static Icon Value => Icon.Value;
}
