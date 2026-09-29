using System.Diagnostics;
using LockedIn.Tracker.Native;

namespace LockedIn.Tracker.Windows;

/// <summary>Called every few seconds, so it avoids allocations and looks each window's process up only once.</summary>
public sealed class Win32ActiveWindowProvider : IActiveWindowProvider
{
    private readonly char[] _titleBuffer = new char[512];
    private readonly char[] _pathBuffer = new char[1024];

    // A window always belongs to the same process, so its app is cached until the foreground window changes.
    private (IntPtr Window, uint ProcessId, string AppName, string? ExecutablePath)? _lastWindow;

    public ActiveWindow? GetActiveWindow()
    {
        var handle = NativeMethods.GetForegroundWindow();
        if (handle == IntPtr.Zero)
            return null;

        NativeMethods.GetWindowThreadProcessId(handle, out var processId);
        if (_lastWindow is not { } last || last.Window != handle || last.ProcessId != processId)
        {
            var path = GetExecutablePath(processId);
            var name = path is null ? GetProcessNameFallback(processId) : Path.GetFileNameWithoutExtension(path);
            if (name is null)
                return null;
            _lastWindow = last = (handle, processId, name, path);
        }

        return new ActiveWindow(last.AppName, GetTitle(handle), last.ExecutablePath);
    }

    /// <summary>
    /// Asks for one process directly, which is much cheaper than Process.GetProcessById (it snapshots them all).
    /// Limited query access works even for elevated apps like Task Manager, unlike Process.MainModule.
    /// </summary>
    private string? GetExecutablePath(uint processId)
    {
        var process = NativeMethods.OpenProcess(NativeMethods.ProcessQueryLimitedInformation, false, processId);
        if (process == IntPtr.Zero)
            return null;

        try
        {
            var size = (uint)_pathBuffer.Length;
            return NativeMethods.QueryFullProcessImageName(process, 0, _pathBuffer, ref size)
                ? new string(_pathBuffer, 0, (int)size)
                : null;
        }
        finally
        {
            NativeMethods.CloseHandle(process);
        }
    }

    /// <summary>For the few system processes that refuse OpenProcess.</summary>
    private static string? GetProcessNameFallback(uint processId)
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

    private string GetTitle(IntPtr handle)
    {
        var length = NativeMethods.GetWindowText(handle, _titleBuffer, _titleBuffer.Length);
        return new string(_titleBuffer, 0, length);
    }
}
