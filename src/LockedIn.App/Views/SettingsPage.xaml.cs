using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LockedIn.App.Controls;
using LockedIn.App.Services;
using LockedIn.Data;

namespace LockedIn.App.Views;

/// <summary>Every change is saved immediately to settings.json (or, for categories, the database).</summary>
public partial class SettingsPage : UserControl, IPage
{
    private static readonly Dictionary<Accent, string> Swatches = new()
    {
        [Accent.Blue] = "#0071E3",
        [Accent.Graphite] = "#6E6E73",
        [Accent.Orange] = "#FF9500",
    };

    private readonly GeneralSettingsService _general;
    private readonly CategorySettingsService _categories;
    private GeneralSettings _settings = null!;

    public SettingsPage(GeneralSettingsService general, CategorySettingsService categories)
    {
        _general = general;
        _categories = categories;
        InitializeComponent();
    }

    public async Task LoadAsync()
    {
        _settings = _general.Get();

        ThemeChoice.Content = Segmented("theme", Enum.GetValues<Theme>().Select(t => (t.ToString(), t)), _settings.Theme,
            theme => Save(_settings with { Theme = theme }));
        IdleChoice.Content = Segmented("idle", GeneralSettings.IdleMinuteChoices.Select(m => ($"{m} min", m)), _settings.IdleMinutes,
            minutes => Save(_settings with { IdleMinutes = minutes }));
        StreakChoice.Content = Segmented("streak", GeneralSettings.StreakToleranceChoices.Select(s => (s < 60 ? $"{s} s" : $"{s / 60} min", s)), _settings.StreakToleranceSeconds,
            seconds => Save(_settings with { StreakToleranceSeconds = seconds }));

        AccentChoice.Children.Clear();
        foreach (var (accent, color) in Swatches)
        {
            var swatch = new RadioButton
            {
                GroupName = "accent",
                Style = (Style)FindResource("Swatch"),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)),
                IsChecked = _settings.Accent == accent,
                ToolTip = accent.ToString(),
            };
            swatch.Checked += (_, _) => Save(_settings with { Accent = accent });
            AccentChoice.Children.Add(swatch);
        }

        Animations.IsChecked = _settings.Animations;
        StoreTitles.IsChecked = _settings.StoreWindowTitles;

        var apps = await _categories.GetAppsAsync(CancellationToken.None);
        Categories.ItemsSource = apps.Select(CategoryRow).ToList();
    }

    private void OnChanged(object sender, RoutedEventArgs e) =>
        Save(_settings with { Animations = Animations.IsChecked == true, StoreWindowTitles = StoreTitles.IsChecked == true });

    private void Save(GeneralSettings settings)
    {
        _settings = settings;
        _general.Save(settings);
    }

    private FrameworkElement CategoryRow(AppCategoryRow app)
    {
        var name = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        name.Children.Add(new TextBlock { Text = app.AppName, FontWeight = FontWeights.Medium, TextTrimming = TextTrimming.CharacterEllipsis });
        name.Children.Add(new TextBlock
        {
            Text = app.IsBrowser ? $"Browser · default {app.DefaultCategory}" : $"Default {app.DefaultCategory}",
            Style = (Style)FindResource("MutedText"),
            FontSize = 12,
        });

        var choices = new[] { ("Default", (Category?)null) }.Concat(Enum.GetValues<Category>().Select(c => (c.ToString(), (Category?)c)));
        var picker = Segmented($"category-{app.AppName}", choices, app.Override,
            async category => await _categories.SetCategoryAsync(app.AppName, category, CancellationToken.None));

        var row = new Grid { Margin = new Thickness(0, 6, 0, 6) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.Children.Add(new AppIconView { AppName = app.AppName, Category = app.Effective, HorizontalAlignment = HorizontalAlignment.Left });
        Grid.SetColumn(name, 1);
        row.Children.Add(name);
        Grid.SetColumn(picker, 2);
        picker.VerticalAlignment = VerticalAlignment.Center;
        row.Children.Add(picker);
        return row;
    }

    /// <summary>A row of pill buttons where exactly one is selected.</summary>
    private Border Segmented<T>(string group, IEnumerable<(string Label, T Value)> choices, T selected, Action<T> onSelect)
    {
        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var (label, value) in choices)
        {
            var button = new RadioButton
            {
                Content = label,
                GroupName = group,
                Style = (Style)FindResource("Segment"),
                IsChecked = EqualityComparer<T>.Default.Equals(value, selected),
            };
            button.Checked += (_, _) => onSelect(value);
            buttons.Children.Add(button);
        }
        return new Border { Style = (Style)FindResource("SegmentGroup"), Child = buttons };
    }
}
