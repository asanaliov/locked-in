using System.Windows;
using System.Windows.Media;

namespace LockedIn.App.Controls;

public sealed record BarSegment(double Value, Brush Brush);

/// <summary>A rounded horizontal bar made of coloured segments, drawn directly instead of with nested elements.</summary>
public sealed class SegmentBar : FrameworkElement
{
    private const double Gap = 3;

    public static readonly DependencyProperty SegmentsProperty = DependencyProperty.Register(
        nameof(Segments), typeof(IReadOnlyList<BarSegment>), typeof(SegmentBar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TotalProperty = DependencyProperty.Register(
        nameof(Total), typeof(double), typeof(SegmentBar),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public IReadOnlyList<BarSegment>? Segments
    {
        get => (IReadOnlyList<BarSegment>?)GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    /// <summary>The value of a full-width bar. Zero means the segments together fill the width.</summary>
    public double Total
    {
        get => (double)GetValue(TotalProperty);
        set => SetValue(TotalProperty, value);
    }

    protected override void OnRender(DrawingContext context)
    {
        if (Segments is not { Count: > 0 } segments)
            return;

        var sum = segments.Sum(s => s.Value);
        var total = Total > 0 ? Total : sum;
        if (total <= 0)
            return;

        var height = ActualHeight;
        var radius = height / 2;
        var x = 0.0;
        foreach (var segment in segments.Where(s => s.Value > 0))
        {
            var width = Math.Max(height, segment.Value / total * ActualWidth - Gap);
            context.DrawRoundedRectangle(segment.Brush, null, new Rect(x, 0, Math.Min(width, ActualWidth - x), height), radius, radius);
            x += width + Gap;
            if (x >= ActualWidth)
                break;
        }
    }
}
