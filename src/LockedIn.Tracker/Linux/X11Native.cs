using System.Runtime.InteropServices;

namespace LockedIn.Tracker.Linux;

/// <summary>The Xlib and XScreenSaver calls the Linux tracker needs. X types are C longs, which are 64-bit here.</summary>
internal static unsafe partial class X11Native
{
    private const string X11 = "libX11.so.6";
    private const string ScreenSaver = "libXss.so.1";

    public const nuint AnyPropertyType = 0;
    public const int Success = 0;

    [LibraryImport(X11)]
    public static partial int XInitThreads();

    [LibraryImport(X11)]
    public static partial IntPtr XOpenDisplay(IntPtr name);

    [LibraryImport(X11)]
    public static partial nuint XDefaultRootWindow(IntPtr display);

    [LibraryImport(X11, StringMarshalling = StringMarshalling.Utf8)]
    public static partial nuint XInternAtom(IntPtr display, string name, int onlyIfExists);

    [LibraryImport(X11)]
    public static partial int XGetWindowProperty(
        IntPtr display, nuint window, nuint property, nint offset, nint length, int delete, nuint requestedType,
        out nuint actualType, out int actualFormat, out nuint itemCount, out nuint bytesAfter, out IntPtr data);

    [LibraryImport(X11)]
    public static partial int XFree(IntPtr data);

    [LibraryImport(X11)]
    public static partial IntPtr XSetErrorHandler(delegate* unmanaged[Cdecl]<IntPtr, IntPtr, int> handler);

    [LibraryImport(ScreenSaver)]
    public static partial int XScreenSaverQueryExtension(IntPtr display, out int eventBase, out int errorBase);

    [LibraryImport(ScreenSaver)]
    public static partial int XScreenSaverQueryInfo(IntPtr display, nuint drawable, ref XScreenSaverInfo info);

    [StructLayout(LayoutKind.Sequential)]
    public struct XScreenSaverInfo
    {
        public nuint Window;
        public int State;
        public int Kind;
        public nuint TilOrSince;
        public nuint Idle;
        public nuint EventMask;
    }
}
