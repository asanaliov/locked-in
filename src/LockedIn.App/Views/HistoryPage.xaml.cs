using System.Windows.Controls;
using System.Windows.Media;
using LockedIn.App.Controls;
using LockedIn.App.Services;
using LockedIn.App.Theming;
using LockedIn.Data;
using LockedIn.Data.Metrics;

namespace LockedIn.App.Views;

public sealed record HistoryDay(string Day, int Score, string Stage, ImageSource StageImage, string Active, string Focus, string Distraction, string Streak);

public sealed record HistoryModel(IReadOnlyList<LegendItem> Legend, IReadOnlyList<HistoryDay> Days);

public partial class HistoryPage : UserControl, IPage
{
    private const int Days = 7;

    private readonly DashboardService _dashboard;

    public HistoryPage(DashboardService dashboard)
    {
        _dashboard = dashboard;
        InitializeComponent();
    }

    public async Task LoadAsync()
    {
        var week = await _dashboard.GetLastDaysAsync(Days, CancellationToken.None);
        var (focus, neutral, distraction) = (ThemeManager.For(Category.Focus), ThemeManager.For(Category.Neutral), ThemeManager.For(Category.Distraction));

        DataContext = new HistoryModel(
            [
                new LegendItem("Focus", "", focus),
                new LegendItem("Neutral", "", neutral),
                new LegendItem("Distraction", "", distraction),
                new LegendItem("Score", "", ThemeManager.Brush("Accent")),
            ],
            week.AsEnumerable().Reverse().Select(day =>
            {
                var stage = LockInStages.For(day.Metrics.Score);
                return new HistoryDay(
                    day.Day.ToString("ddd d MMM"),
                    day.Metrics.Score,
                    stage.Name,
                    Art.Stage(stage.Level, 72),
                    Format.Duration(day.Metrics.ActiveTime),
                    Format.Duration(day.Metrics.FocusTime),
                    Format.Duration(day.Metrics.DistractionTime),
                    Format.Duration(day.Metrics.LongestStreak));
            }).ToList());

        WeekChart.SetData(
            week.Select(day => new ChartColumn(
                day.Day.ToString("ddd d"),
                [
                    new BarSegment(day.Metrics.FocusTime.TotalHours, focus),
                    new BarSegment(day.Metrics.NeutralTime.TotalHours, neutral),
                    new BarSegment(day.Metrics.DistractionTime.TotalHours, distraction),
                ],
                $"""
                {day.Day:dddd d MMM} · score {day.Metrics.Score}
                Active {Format.Exact(day.Metrics.ActiveTime)}
                Focus {Format.Exact(day.Metrics.FocusTime)}
                Neutral {Format.Exact(day.Metrics.NeutralTime)}
                Distraction {Format.Exact(day.Metrics.DistractionTime)}
                """)).ToList(),
            hours => $"{hours:0.#}h",
            line: week.Select(day => (double)day.Metrics.Score).ToList());
    }
}
