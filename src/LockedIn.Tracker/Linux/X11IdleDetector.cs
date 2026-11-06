using LockedIn.Tracker.Windows;
using Microsoft.Extensions.Logging;

namespace LockedIn.Tracker.Linux;

/// <summary>Time since the last keyboard or mouse input, from the X server's screen saver extension.</summary>
public sealed class X11IdleDetector(X11Connection x11, ILogger<X11IdleDetector> logger) : IIdleDetector
{
    private bool _unavailable;

    public TimeSpan GetIdleTime()
    {
        if (_unavailable || !x11.IsOpen)
            return TimeSpan.Zero;

        try
        {
            if (X11Native.XScreenSaverQueryExtension(x11.Display, out _, out _) == 0)
            {
                Disable("the X server has no screen saver extension");
                return TimeSpan.Zero;
            }

            var info = default(X11Native.XScreenSaverInfo);
            return X11Native.XScreenSaverQueryInfo(x11.Display, x11.Root, ref info) != 0
                ? TimeSpan.FromMilliseconds(info.Idle)
                : TimeSpan.Zero;
        }
        catch (DllNotFoundException)
        {
            Disable("libXss.so.1 is not installed");
            return TimeSpan.Zero;
        }
    }

    private void Disable(string reason)
    {
        _unavailable = true;
        logger.LogWarning("Idle time can't be detected because {Reason}, so time away from the computer will count as active", reason);
    }
}
