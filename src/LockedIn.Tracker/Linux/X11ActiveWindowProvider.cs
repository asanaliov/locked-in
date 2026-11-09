using System.Text;
using LockedIn.Tracker.Windows;

namespace LockedIn.Tracker.Linux;

/// <summary>
/// Reads the focused window from the standard properties every X11 window manager publishes.
/// On a Wayland session this only sees apps running through XWayland; see <see cref="LinuxSession"/>.
/// </summary>
public sealed class X11ActiveWindowProvider(X11Connection x11) : IActiveWindowProvider
{
    private (nuint Window, string AppName)? _lastWindow;

    public ActiveWindow? GetActiveWindow()
    {
        if (!x11.IsOpen || x11.ReadCardinal(x11.Root, "_NET_ACTIVE_WINDOW") is not { } window || window == 0)
            return null;

        // A window keeps its app, so that's only looked up when the focus moves.
        if (_lastWindow is not { } last || last.Window != window)
        {
            if (GetAppName(window) is not { } name)
                return null; // closed during the lookup
            _lastWindow = last = (window, name);
        }

        return new ActiveWindow(last.AppName, GetTitle(window));
    }

    /// <summary>
    /// The window class first, because it's the same whether an app came from a package, a snap or a flatpak,
    /// and because the process of a sandboxed app isn't visible from here.
    /// </summary>
    private string? GetAppName(nuint window)
    {
        var windowClass = x11.ReadText(window, "WM_CLASS", Encoding.Latin1);
        if (AppNameFromClass(windowClass) is { } name)
            return name;

        return x11.ReadCardinal(window, "_NET_WM_PID") is { } processId ? ProcessName((int)processId) : null;
    }

    /// <summary>WM_CLASS is "instance\0class\0"; the class names the app ("Navigator\0firefox\0").</summary>
    public static string? AppNameFromClass(string? windowClass)
    {
        var parts = windowClass?.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        return parts is { Length: > 0 } ? parts[^1] : null;
    }

    private static string? ProcessName(int processId)
    {
        try
        {
            return File.ReadAllText($"/proc/{processId}/comm").TrimEnd('\n');
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private string GetTitle(nuint window) =>
        x11.ReadText(window, "_NET_WM_NAME", Encoding.UTF8) ?? x11.ReadText(window, "WM_NAME", Encoding.Latin1) ?? "";
}
