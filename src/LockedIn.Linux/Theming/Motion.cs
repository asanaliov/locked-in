using Avalonia.Threading;

namespace LockedIn.Linux.Theming;

/// <summary>Short one-off animations that respect the Animations setting and leave no timer running afterwards.</summary>
internal static class Motion
{
    /// <summary>Calls <paramref name="set"/> with values from 0 to 1, easing out, ending on exactly 1.</summary>
    public static void Animate(TimeSpan duration, Action<double> set)
    {
        if (!ThemeManager.Animations)
        {
            set(1);
            return;
        }

        var started = DateTime.UtcNow;
        var timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(16) };
        timer.Tick += (_, _) =>
        {
            var progress = Math.Min(1, (DateTime.UtcNow - started) / duration);
            set(1 - Math.Pow(1 - progress, 4));
            if (progress >= 1)
                timer.Stop();
        };
        set(0);
        timer.Start();
    }
}
