using System.Windows;
using System.Windows.Media;

namespace LockedIn.App.Controls;

/// <summary>The Locked In Score as a ring that fills clockwise from the top.</summary>
public sealed class ScoreRing : FrameworkElement
{
    private const double Thickness = 12;

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(ScoreRing),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>0 to 100. Animatable.</summary>
    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    protected override void OnRender(DrawingContext context)
    {
        var size = Math.Min(ActualWidth, ActualHeight);
        var radius = (size - Thickness) / 2;
        var center = new Point(ActualWidth / 2, ActualHeight / 2);

        var track = new Pen((Brush)FindResource("Raised"), Thickness);
        context.DrawEllipse(null, track, center, radius, radius);

        var fraction = Math.Clamp(Value / 100, 0, 1);
        if (fraction <= 0)
            return;

        var pen = new Pen((Brush)FindResource("Accent"), Thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        if (fraction >= 0.9999)
        {
            context.DrawEllipse(null, pen, center, radius, radius);
            return;
        }

        var angle = fraction * 2 * Math.PI;
        var start = new Point(center.X, center.Y - radius);
        var end = new Point(center.X + radius * Math.Sin(angle), center.Y - radius * Math.Cos(angle));
        var arc = new StreamGeometry();
        using (var geometry = arc.Open())
        {
            geometry.BeginFigure(start, isFilled: false, isClosed: false);
            geometry.ArcTo(end, new Size(radius, radius), 0, isLargeArc: fraction > 0.5, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: false);
        }
        arc.Freeze();
        context.DrawGeometry(null, pen, arc);
    }
}
