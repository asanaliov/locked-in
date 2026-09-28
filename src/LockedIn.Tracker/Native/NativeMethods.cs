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

    [StructLayout(LayoutKind.Sequential)]
    internal struct LastInputInfo
    {
        public static readonly uint StructSize = (uint)Marshal.SizeOf<LastInputInfo>();

        public uint Size;
        public uint Time;
    }
}
