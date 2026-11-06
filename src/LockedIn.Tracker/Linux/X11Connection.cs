using System.Runtime.InteropServices;
using System.Text;

namespace LockedIn.Tracker.Linux;

/// <summary>
/// The tracker's own connection to the X server, shared by the window and idle lookups.
/// Only the tracker's tick uses it, one call at a time.
/// </summary>
public sealed unsafe class X11Connection
{
    private readonly Dictionary<string, nuint> _atoms = [];

    public X11Connection()
    {
        // Must come before any other Xlib call in the process, and the window toolkit opens its own connection later.
        X11Native.XInitThreads();

        // Xlib's default handler ends the process on any error, and a window closing mid-lookup is a normal BadWindow.
        X11Native.XSetErrorHandler(&IgnoreError);

        Display = X11Native.XOpenDisplay(IntPtr.Zero);
        Root = Display == IntPtr.Zero ? 0 : X11Native.XDefaultRootWindow(Display);
    }

    /// <summary>Zero when there is no X server to talk to.</summary>
    public IntPtr Display { get; }

    public nuint Root { get; }

    public bool IsOpen => Display != IntPtr.Zero;

    /// <summary>The first value of a window-sized (32-bit format) property, like a window id or a process id.</summary>
    public nuint? ReadCardinal(nuint window, string property)
    {
        if (!TryRead(window, property, out var format, out var count, out var data))
            return null;

        try
        {
            // Format 32 values are handed back as C longs, whatever their size on the wire.
            return format == 32 && count > 0 ? (nuint)Marshal.ReadIntPtr(data) : null;
        }
        finally
        {
            X11Native.XFree(data);
        }
    }

    /// <summary>A text property as bytes decoded with the given encoding, or null if the window doesn't have it.</summary>
    public string? ReadText(nuint window, string property, Encoding encoding)
    {
        if (!TryRead(window, property, out var format, out var count, out var data))
            return null;

        try
        {
            return format == 8 ? encoding.GetString((byte*)data, checked((int)count)) : null;
        }
        finally
        {
            X11Native.XFree(data);
        }
    }

    private bool TryRead(nuint window, string property, out int format, out nuint count, out IntPtr data)
    {
        // The length is in 32-bit units, so this reads up to 16 KB: far more than any title.
        var status = X11Native.XGetWindowProperty(
            Display, window, Atom(property), 0, 4096, 0, X11Native.AnyPropertyType,
            out var type, out format, out count, out _, out data);

        if (status != X11Native.Success || data == IntPtr.Zero)
        {
            data = IntPtr.Zero;
            count = 0;
            return false;
        }

        if (type != 0)
            return true;

        X11Native.XFree(data);
        data = IntPtr.Zero;
        count = 0;
        return false;
    }

    private nuint Atom(string name)
    {
        if (!_atoms.TryGetValue(name, out var atom))
            _atoms[name] = atom = X11Native.XInternAtom(Display, name, 0);
        return atom;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static int IgnoreError(IntPtr display, IntPtr error) => 0;
}
