using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace LockedIn.App.Controls;

/// <param name="Segments">Stacked from the bottom up.</param>
/// <param name="Tooltip">Shown next to the pointer while hovering the column.</param>
public sealed record ChartColumn(string Label, IReadOnlyList<BarSegment> Segments, string Tooltip);

/// <summary>
/// Column chart drawn in one pass: optionally stacked columns, a dashed reference line and a line series
/// with its own 0-100 scale. Everything is set once after the data loads, so it only redraws on resize.
/// </summary>
public sealed class ColumnChart : FrameworkElement
{
    private const double AxisWidth = 44;
    private const double LabelHeight = 22;
    private const double LineAxisWidth = 30;
    private const int GridLines = 4;

    private IReadOnlyList<ChartColumn> _columns = [];
    private IReadOnlyList<double>? _line;
    private double? _reference;
    private double _max;
    private Func<double, string> _formatAxis = v => v.ToString("0", CultureInfo.CurrentCulture);
    private int _labelEvery = 1;
    private int? _hovered;

    // One tooltip that follows the pointer from column to column; a plain ToolTip property only opens on entering the chart.
    private readonly ToolTip _tooltip = new() { Placement = PlacementMode.Relative };

    public ColumnChart() => _tooltip.PlacementTarget = this;

    public void SetData(
        IReadOnlyList<ChartColumn> columns,
        Func<double, string> formatAxis,
        double minimumMax = 0,
        double? reference = null,
        IReadOnlyList<double>? line = null,
        int labelEvery = 1,
        IReadOnlyList<double>? gridSteps = null)
    {
        _columns = columns;
        _formatAxis = formatAxis;
        _reference = reference;
        _line = line;
        _labelEvery = labelEvery;

        var highest = Math.Max(columns.Select(c => c.Segments.Sum(s => s.Value)).DefaultIfEmpty(0).Max(), reference ?? 0);
        highest = Math.Max(highest, minimumMax);
        // With known units (like minutes), pick the first step that fits so grid lines land on 15m, 30m, 1h...
        var step = gridSteps?.FirstOrDefault(s => s * GridLines >= highest) ?? 0;
        _max = step > 0 ? step * GridLines : NiceMax(highest);
        InvalidateVisual();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var position = e.GetPosition(this);
        var index = ColumnAt(position.X);
        if (index != _hovered)
        {
            _hovered = index;
            InvalidateVisual();
        }

        if (index is { } i)
        {
            _tooltip.Content = _columns[i].Tooltip;
            _tooltip.HorizontalOffset = position.X + 14;
            _tooltip.VerticalOffset = position.Y + 18;
            _tooltip.IsOpen = true;
        }
        else
        {
            _tooltip.IsOpen = false;
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _tooltip.IsOpen = false;
        _hovered = null;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext context)
    {
        if (_columns.Count == 0 || _max <= 0)
            return;

        var muted = (Brush)FindResource("Muted");
        var gridPen = new Pen((Brush)FindResource("Border"), 1);
        var plot = PlotArea();
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var slot = plot.Width / _columns.Count;

        // Transparent fill so the pointer is tracked between the bars too, not only over them.
        context.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
        if (_hovered is { } hovered)
            context.DrawRoundedRectangle((Brush)FindResource("Raised"), null, new Rect(plot.Left + slot * hovered + 2, plot.Top, slot - 4, plot.Height), 6, 6);

        // Background context: horizontal grid lines with their values.
        for (var i = 0; i <= GridLines; i++)
        {
            var value = _max * i / GridLines;
            var y = Math.Round(plot.Bottom - value / _max * plot.Height) + 0.5;
            context.DrawLine(gridPen, new Point(plot.Left, y), new Point(plot.Right, y));
            var text = Text(_formatAxis(value), muted, dpi);
            context.DrawText(text, new Point(plot.Left - text.Width - 8, y - text.Height / 2));
        }

        var barWidth = Math.Min(slot * 0.6, 34);
        for (var i = 0; i < _columns.Count; i++)
        {
            var column = _columns[i];
            var x = plot.Left + slot * i + (slot - barWidth) / 2;
            var bottom = plot.Bottom;
            foreach (var segment in column.Segments.Where(s => s.Value > 0))
            {
                var height = segment.Value / _max * plot.Height;
                var radius = Math.Min(4, Math.Min(barWidth, height) / 2);
                context.DrawRoundedRectangle(segment.Brush, null, new Rect(x, bottom - height, barWidth, height), radius, radius);
                bottom -= height;
            }

            if (i % _labelEvery == 0)
            {
                var label = Text(column.Label, muted, dpi);
                context.DrawText(label, new Point(plot.Left + slot * i + (slot - label.Width) / 2, plot.Bottom + 5));
            }
        }

        if (_reference is { } reference)
        {
            var y = plot.Bottom - reference / _max * plot.Height;
            var dashed = new Pen(muted, 1.5) { DashStyle = new DashStyle([3, 3], 0) };
            context.DrawLine(dashed, new Point(plot.Left, y), new Point(plot.Right, y));
        }

        if (_line is { Count: > 0 } line)
            DrawLine(context, line, plot, slot, muted, dpi);
    }

    private void DrawLine(DrawingContext context, IReadOnlyList<double> line, Rect plot, double slot, Brush muted, double dpi)
    {
        var accent = (Brush)FindResource("Accent");
        var points = line.Select((value, i) => new Point(plot.Left + slot * (i + 0.5), plot.Bottom - value / 100 * plot.Height)).ToList();

        var geometry = new StreamGeometry();
        using (var figure = geometry.Open())
        {
            figure.BeginFigure(points[0], isFilled: false, isClosed: false);
            figure.PolyLineTo(points.Skip(1).ToList(), isStroked: true, isSmoothJoin: true);
        }
        geometry.Freeze();
        context.DrawGeometry(null, new Pen(accent, 2.5) { LineJoin = PenLineJoin.Round }, geometry);

        var card = (Brush)FindResource("Card");
        var outline = new Pen(accent, 2);
        foreach (var point in points)
            context.DrawEllipse(card, outline, point, 4, 4);

        foreach (var score in new[] { 0, 50, 100 })
        {
            var text = Text(score.ToString(CultureInfo.CurrentCulture), muted, dpi);
            context.DrawText(text, new Point(plot.Right + 8, plot.Bottom - score / 100.0 * plot.Height - text.Height / 2));
        }
    }

    private Rect PlotArea()
    {
        var right = _line is null ? 0 : LineAxisWidth;
        return new Rect(AxisWidth, 6, Math.Max(0, ActualWidth - AxisWidth - right), Math.Max(0, ActualHeight - LabelHeight - 6));
    }

    private int? ColumnAt(double x)
    {
        var plot = PlotArea();
        if (_columns.Count == 0 || x < plot.Left || x >= plot.Right)
            return null;
        return Math.Min(_columns.Count - 1, (int)((x - plot.Left) / (plot.Width / _columns.Count)));
    }

    private FormattedText Text(string text, Brush brush, double pixelsPerDip) =>
        new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 11, brush, pixelsPerDip);

    /// <summary>Rounds the axis maximum up to 1, 2 or 5 times a power of ten, so the grid lines land on tidy values.</summary>
    private static double NiceMax(double value)
    {
        if (value <= 0)
            return 1;
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(value)));
        foreach (var step in new[] { 1.0, 2.0, 2.5, 5.0, 10.0 })
        {
            if (step * magnitude >= value)
                return step * magnitude;
        }
        return 10 * magnitude;
    }
}
