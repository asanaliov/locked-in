using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using LockedIn.App.Controls;
using LockedIn.App.Services;
using LockedIn.App.Theming;
using LockedIn.Data;
using LockedIn.Data.Metrics;

namespace LockedIn.App.Views;

public sealed record StageItem(string Name, string Range, ImageSource Image, double Opacity, Brush Outline);

public sealed record LegendItem(string Name, string Time, Brush Brush);

public sealed record TodayModel(
    string Date,
    string ActiveTime,
    string FocusRatio,
    string LongestStreak,
    string SwitchesPerHour,
    bool HasData,
    ImageSource? EmptyImage,
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

        Motion.Animate(Ring, ScoreRing.ValueProperty, 0, metrics.Score, TimeSpan.FromMilliseconds(1200), new QuarticEase());
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
