using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using LockedIn.Core.Services;
using LockedIn.Data;
using LockedIn.Data.Metrics;
using LockedIn.Linux.Controls;
using LockedIn.Linux.Theming;
using LockedIn.Tracker.Linux;

namespace LockedIn.Linux.Views;

public sealed record StageItem(string Name, string Range, Bitmap Image, double Opacity, IBrush Outline);

public sealed record LegendItem(string Name, string Time, IBrush Brush);

public sealed record TodayModel(
    string Date,
    string ActiveTime,
    string FocusRatio,
    string LongestStreak,
    string SwitchesPerHour,
    bool HasData,
    bool IsWayland,
    Bitmap? EmptyImage,
    string StageName,
    IReadOnlyList<StageItem> Stages,
    IReadOnlyList<BarSegment> Breakdown,
    IReadOnlyList<LegendItem> Legend,
    IReadOnlyList<AppRow> Apps)
{
    public bool IsEmpty => !HasData;
}

public partial class TodayPage : UserControl, IPage
{
    private readonly DashboardService _dashboard;

    public TodayPage(DashboardService dashboard)
    {
        _dashboard = dashboard;
        InitializeComponent();
    }

    public async Task LoadAsync()
    {
        var metrics = await _dashboard.GetDayAsync(DashboardService.Today, CancellationToken.None);
        DataContext = Build(metrics);

        Motion.Animate(TimeSpan.FromMilliseconds(1200), progress => Ring.Value = metrics.Score * progress);
    }

    private static TodayModel Build(DayMetrics metrics)
    {
        var hasData = metrics.ActiveTime > TimeSpan.Zero;
        var current = LockInStages.For(metrics.Score);
        var parts = new[]
        {
            (Category: Category.Focus, Time: metrics.FocusTime),
            (Category: Category.Neutral, Time: metrics.NeutralTime),
            (Category: Category.Distraction, Time: metrics.DistractionTime),
        };

        return new TodayModel(
            DateTime.Now.ToString("dddd, d MMMM"),
            Format.Duration(metrics.ActiveTime),
            Format.Percent(metrics.FocusRatio),
            Format.Duration(metrics.LongestStreak),
            metrics.SwitchesPerHour.ToString("0.#"),
            hasData,
            LinuxSession.IsWayland,
            hasData ? null : Art.EmptyState(),
            current.Name,
            hasData
                ? LockInStages.All.Select(stage => new StageItem(
                    stage.Name,
                    $"{stage.MinScore}–{LockInStages.MaxScore(stage)}",
                    Art.Stage(stage.Level, 360),
                    stage == current ? 1 : 0.45,
                    stage == current ? ThemeManager.Brush("Accent") : Brushes.Transparent)).ToList()
                : [],
            parts.Select(p => new BarSegment(p.Time.TotalMinutes, ThemeManager.For(p.Category))).ToList(),
            parts.Select(p => new LegendItem(p.Category.ToString(), Format.Duration(p.Time), ThemeManager.For(p.Category))).ToList(),
            AppRow.From(metrics.Apps.Take(15).ToList(), metrics.ActiveTime));
    }
}
