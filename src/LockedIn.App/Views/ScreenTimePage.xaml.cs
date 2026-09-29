using System.Windows.Controls;
using System.Windows.Media;
using LockedIn.App.Controls;
using LockedIn.App.Services;
using LockedIn.App.Theming;
using LockedIn.Data;

namespace LockedIn.App.Views;

public sealed record ScreenTimeModel(string Total, string Comparison, string Average, string MostUsed, IReadOnlyList<AppRow> Apps)
{
    public bool IsEmpty => Apps.Count == 0;
}

public partial class ScreenTimePage : UserControl, IPage
{
    private const int Days = 7;

    /// <summary>Grid line spacing for minute axes: 15m, 30m, 1h, 2h... so labels stay round.</summary>
    private static readonly double[] MinuteSteps = [15, 30, 60, 120, 180, 240, 360];

    private readonly DashboardService _dashboard;

    public ScreenTimePage(DashboardService dashboard)
    {
        _dashboard = dashboard;
        InitializeComponent();
    }

    public async Task LoadAsync()
    {
        var week = await _dashboard.GetLastDaysAsync(Days, CancellationToken.None);
        var hours = await _dashboard.GetMinutesPerHourAsync(DashboardService.Today, CancellationToken.None);
        var today = week[^1].Metrics;

        // Averaged over the days with any tracked time, so a fresh install isn't dragged down by empty days.
        var tracked = week.Where(d => d.Metrics.ActiveTime > TimeSpan.Zero).ToList();
        var average = tracked.Count == 0 ? TimeSpan.Zero : tracked.Aggregate(TimeSpan.Zero, (sum, d) => sum + d.Metrics.ActiveTime) / tracked.Count;
        var difference = today.ActiveTime - average;

        var accent = ThemeManager.Brush("Accent");
        DataContext = new ScreenTimeModel(
            Format.Duration(today.ActiveTime),
            average > TimeSpan.Zero && difference.Duration() >= TimeSpan.FromMinutes(1)
                ? $"Today · {Format.Duration(difference.Duration())} {(difference > TimeSpan.Zero ? "above" : "below")} your daily average"
                : "Today",
            Format.Duration(average),
            today.Apps.FirstOrDefault()?.AppName ?? "–",
            AppRow.From(today.Apps, today.ActiveTime, _ => accent));

        HoursChart.SetData(
            hours.Select((minutes, hour) => new ChartColumn(HourLabel(hour), [new BarSegment(minutes, accent)], $"{HourRange(hour)} · {Exact(minutes)}")).ToList(),
            AxisMinutes,
            minimumMax: 60,
            labelEvery: 3,
            gridSteps: MinuteSteps);

        var faded = Faded(accent);
        WeekChart.SetData(
            week.Select((day, i) => new ChartColumn(
                day.Day.ToString("ddd"),
                [new BarSegment(day.Metrics.ActiveTime.TotalMinutes, i == week.Count - 1 ? accent : faded)],
                $"{day.Day:dddd d MMM} · {Format.Exact(day.Metrics.ActiveTime)}")).ToList(),
            AxisMinutes,
            reference: average > TimeSpan.Zero ? average.TotalMinutes : null,
            gridSteps: MinuteSteps);
    }

    private static string HourLabel(int hour) => hour switch
    {
        0 => "12a",
        < 12 => $"{hour}a",
        12 => "12p",
        _ => $"{hour - 12}p",
    };

    /// <summary>"9 AM – 10 AM", in the user's own clock format.</summary>
    private static string HourRange(int hour) =>
        $"{DateTime.Today.AddHours(hour):h tt} – {DateTime.Today.AddHours(hour + 1):h tt}";

    private static string Minutes(double minutes) => Format.Duration(TimeSpan.FromMinutes(minutes));

    private static string Exact(double minutes) => Format.Exact(TimeSpan.FromMinutes(minutes));

    /// <summary>Axis labels land on round values, so whole hours read "3h" instead of "3h 00m".</summary>
    private static string AxisMinutes(double minutes) =>
        minutes >= 60 && minutes % 60 == 0 ? $"{minutes / 60:0}h" : Minutes(minutes);

    private static Brush Faded(Brush brush)
    {
        var faded = brush.Clone();
        faded.Opacity = 0.4;
        faded.Freeze();
        return faded;
    }
}
