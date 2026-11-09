using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace LockedIn.Linux.Controls;

public sealed record BarSegment(double Value, IBrush Brush);

/// <summary>A rounded horizontal bar made of coloured segments, drawn directly instead of with nested elements.</summary>
public sealed class SegmentBar : Control
{
    private const double Gap = 3;

    public static readonly StyledProperty<IReadOnlyList<BarSegment>?> SegmentsProperty =
        AvaloniaProperty.Register<SegmentBar, IReadOnlyList<BarSegment>?>(nameof(Segments));

    public static readonly StyledProperty<double> TotalProperty = AvaloniaProperty.Register<SegmentBar, double>(nameof(Total));

    static SegmentBar() => AffectsRender<SegmentBar>(SegmentsProperty, TotalProperty);

    public IReadOnlyList<BarSegment>? Segments
    {
        get => GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    /// <summary>The value of a full-width bar. Zero means the segments together fill the width.</summary>
    public double Total
    {
        get => GetValue(TotalProperty);
        set => SetValue(TotalProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        if (Segments is not { Count: > 0 } segments)
            return;

        var sum = segments.Sum(s => s.Value);
        var total = Total > 0 ? Total : sum;
        if (total <= 0)
            return;

        var height = Bounds.Height;
        var radius = height / 2;
        var x = 0.0;
        foreach (var segment in segments.Where(s => s.Value > 0))
        {
            var width = Math.Max(height, segment.Value / total * Bounds.Width - Gap);
            context.DrawRectangle(segment.Brush, null, new Rect(x, 0, Math.Min(width, Bounds.Width - x), height), radius, radius);
            x += width + Gap;
            if (x >= Bounds.Width)
                break;
        }
    }
}
