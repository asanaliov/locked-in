using System.Runtime.InteropServices;

namespace LockedIn.App;

/// <summary>All of the app's own Win32 P/Invoke declarations live here.</summary>
internal static partial class NativeMethods
{
    internal const int DwmwaUseImmersiveDarkMode = 20;
    internal const int DwmwaBorderColor = 34;
    internal const int DwmwaCaptionColor = 35;
    internal const int DwmwaTextColor = 36;

    [LibraryImport("dwmapi.dll")]
    internal static partial int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [LibraryImport("psapi.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EmptyWorkingSet(IntPtr process);
}
