using LockedIn.Tracker.Native;

namespace LockedIn.Tracker.Windows;

public sealed class Win32IdleDetector : IIdleDetector
{
    public TimeSpan GetIdleTime()
    {
        var info = new NativeMethods.LastInputInfo { Size = NativeMethods.LastInputInfo.StructSize };
        if (!NativeMethods.GetLastInputInfo(ref info))
            return TimeSpan.Zero;

        // Both values are 32-bit tick counts; unsigned subtraction handles the ~49 day wrap-around.
        var idleMilliseconds = unchecked((uint)Environment.TickCount - info.Time);
        return TimeSpan.FromMilliseconds(idleMilliseconds);
    }
}
