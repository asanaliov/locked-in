using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace LockedIn.Linux.Controls;

/// <summary>The Locked In Score as a ring that fills clockwise from the top.</summary>
public sealed class ScoreRing : Control
{
    private const double Thickness = 12;

    public static readonly StyledProperty<double> ValueProperty = AvaloniaProperty.Register<ScoreRing, double>(nameof(Value));

    static ScoreRing() => AffectsRender<ScoreRing>(ValueProperty);

    /// <summary>0 to 100.</summary>
    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var size = Math.Min(Bounds.Width, Bounds.Height);
        var radius = (size - Thickness) / 2;
        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);

        context.DrawEllipse(null, new Pen((IBrush?)this.FindResource("Raised"), Thickness), center, radius, radius);

        var fraction = Math.Clamp(Value / 100, 0, 1);
        if (fraction <= 0)
            return;

        var pen = new Pen((IBrush?)this.FindResource("Accent"), Thickness, lineCap: PenLineCap.Round);
        if (fraction >= 0.9999)
        {
            context.DrawEllipse(null, pen, center, radius, radius);
            return;
        }

        var angle = fraction * 2 * Math.PI;
        var arc = new StreamGeometry();
        using (var geometry = arc.Open())
        {
            geometry.BeginFigure(new Point(center.X, center.Y - radius), isFilled: false);
            geometry.ArcTo(
                new Point(center.X + radius * Math.Sin(angle), center.Y - radius * Math.Cos(angle)),
                new Size(radius, radius), 0, isLargeArc: fraction > 0.5, SweepDirection.Clockwise);
            geometry.EndFigure(isClosed: false);
        }
        context.DrawGeometry(null, pen, arc);
    }
}
