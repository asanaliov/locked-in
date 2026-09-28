using System.Diagnostics;
using LockedIn.Tracker.Native;

namespace LockedIn.Tracker.Windows;

public sealed class Win32ActiveWindowProvider : IActiveWindowProvider
{
    private const int MaxTitleLength = 512;
    private const int MaxPathLength = 1024;

    public ActiveWindow? GetActiveWindow()
    {
        var handle = NativeMethods.GetForegroundWindow();
        if (handle == IntPtr.Zero)
            return null;

        NativeMethods.GetWindowThreadProcessId(handle, out var processId);
        var appName = GetProcessName(processId);
        return appName is null ? null : new ActiveWindow(appName, GetTitle(handle), GetExecutablePath(processId));
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

    /// <summary>Limited query access works even for elevated apps like Task Manager, unlike Process.MainModule.</summary>
    private static string? GetExecutablePath(uint processId)
    {
        var process = NativeMethods.OpenProcess(NativeMethods.ProcessQueryLimitedInformation, false, processId);
        if (process == IntPtr.Zero)
            return null;

        try
        {
            var buffer = new char[MaxPathLength];
            var size = (uint)buffer.Length;
            return NativeMethods.QueryFullProcessImageName(process, 0, buffer, ref size) ? new string(buffer, 0, (int)size) : null;
        }
        finally
        {
            NativeMethods.CloseHandle(process);
        }
    }

    private static string GetTitle(IntPtr handle)
    {
        var buffer = new char[MaxTitleLength];
        var length = NativeMethods.GetWindowText(handle, buffer, buffer.Length);
        return new string(buffer, 0, length);
    }
}
