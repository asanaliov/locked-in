using System.Diagnostics;
using LockedIn.Tracker.Native;

namespace LockedIn.Tracker.Windows;

public sealed class Win32ActiveWindowProvider : IActiveWindowProvider
{
    private const int MaxTitleLength = 512;

    public ActiveWindow? GetActiveWindow()
    {
        var handle = NativeMethods.GetForegroundWindow();
        if (handle == IntPtr.Zero)
            return null;

        NativeMethods.GetWindowThreadProcessId(handle, out var processId);
        var appName = GetProcessName(processId);
        return appName is null ? null : new ActiveWindow(appName, GetTitle(handle));
    }

    private static string? GetProcessName(uint processId)
    {
        try
        {
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch (ArgumentException)
        {
            return null; // process exited between the two calls
        }
    }

    private static string GetTitle(IntPtr handle)
    {
        var buffer = new char[MaxTitleLength];
        var length = NativeMethods.GetWindowText(handle, buffer, buffer.Length);
        return new string(buffer, 0, length);
    }
}
