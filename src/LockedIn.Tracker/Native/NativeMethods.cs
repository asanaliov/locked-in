using System.Runtime.InteropServices;

namespace LockedIn.Tracker.Native;

/// <summary>All Win32 P/Invoke declarations live here.</summary>
internal static partial class NativeMethods
{
    [LibraryImport("user32.dll")]
    internal static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll")]
    internal static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int GetWindowText(IntPtr hWnd, [Out] char[] text, int maxCount);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetLastInputInfo(ref LastInputInfo info);

    internal const uint ProcessQueryLimitedInformation = 0x1000;

    [LibraryImport("kernel32.dll")]
    internal static partial IntPtr OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint processId);

    [LibraryImport("kernel32.dll", EntryPoint = "QueryFullProcessImageNameW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool QueryFullProcessImageName(IntPtr process, uint flags, [Out] char[] path, ref uint size);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool CloseHandle(IntPtr handle);

    [StructLayout(LayoutKind.Sequential)]
    internal struct LastInputInfo
    {
        public static readonly uint StructSize = (uint)Marshal.SizeOf<LastInputInfo>();

        public uint Size;
        public uint Time;
    }
}
