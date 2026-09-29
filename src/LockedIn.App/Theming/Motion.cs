using System.Windows;
using System.Windows.Media.Animation;

namespace LockedIn.App.Theming;

/// <summary>Short one-off animations that respect the Animations setting and leave no clock running afterwards.</summary>
internal static class Motion
{
    public static void Animate(UIElement target, DependencyProperty property, double from, double to, TimeSpan duration, IEasingFunction? easing = null)
    {
        if (!ThemeManager.Animations)
        {
            target.SetValue(property, to);
            return;
        }

        var animation = new DoubleAnimation(from, to, duration) { EasingFunction = easing, FillBehavior = FillBehavior.Stop };
        // Set the final value first so it stays once the animation stops and releases its clock.
        target.SetValue(property, to);
        target.BeginAnimation(property, animation);
    }
}
