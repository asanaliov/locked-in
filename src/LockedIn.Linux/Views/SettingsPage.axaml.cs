using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using LockedIn.Core.Services;
using LockedIn.Core.Updates;
using LockedIn.Data;
using LockedIn.Linux.Controls;
using LockedIn.Linux.Tray;

namespace LockedIn.Linux.Views;

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
    private readonly UpdateService _updates;
    private GeneralSettings _settings = null!;

    public SettingsPage(GeneralSettingsService general, CategorySettingsService categories, UpdateService updates)
    {
        _general = general;
        _categories = categories;
        _updates = updates;
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
                Classes = { "swatch" },
                Background = new SolidColorBrush(Color.Parse(color)),
                IsChecked = _settings.Accent == accent,
            };
            ToolTip.SetTip(swatch, accent.ToString());
            swatch.IsCheckedChanged += (_, _) =>
            {
                if (swatch.IsChecked == true)
                    Save(_settings with { Accent = accent });
            };
            AccentChoice.Children.Add(swatch);
        }

        Animations.IsChecked = _settings.Animations;
        StoreTitles.IsChecked = _settings.StoreWindowTitles;
        CheckForUpdates.IsChecked = _settings.CheckForUpdates;
        StartAtLogin.IsChecked = Autostart.IsEnabled();
        VersionText.Text = $"Locked In {UpdateService.CurrentVersion.ToString(3)}";
        ShowUpdate(_updates.Available, checkedJustNow: false);

        var apps = await _categories.GetAppsAsync(CancellationToken.None);
        Categories.ItemsSource = apps.Select(CategoryRow).ToList();
    }

    private void OnChanged(object? sender, RoutedEventArgs e) =>
        Save(_settings with
        {
            Animations = Animations.IsChecked == true,
            StoreWindowTitles = StoreTitles.IsChecked == true,
            CheckForUpdates = CheckForUpdates.IsChecked == true,
        });

    private void OnStartAtLogin(object? sender, RoutedEventArgs e)
    {
        Autostart.SetEnabled(StartAtLogin.IsChecked == true);
        StartAtLogin.IsChecked = Autostart.IsEnabled();
    }

    private async void OnCheckNow(object? sender, RoutedEventArgs e)
    {
        CheckNow.IsEnabled = false;
        UpdateStatus.Text = "Checking…";
        try
        {
            ShowUpdate(await _updates.CheckAsync(CancellationToken.None), checkedJustNow: true);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            UpdateStatus.Text = "Couldn't reach GitHub. Check your connection and try again.";
        }
        finally
        {
            CheckNow.IsEnabled = true;
        }
    }

    private void OnDownload(object? sender, RoutedEventArgs e) => AppController.OpenLink(ReleaseFeed.LatestReleasePage);

    private void ShowUpdate(AvailableUpdate? update, bool checkedJustNow)
    {
        UpdateStatus.Text = update is not null
            ? $"Version {update.Version.ToString(3)} is available."
            : checkedJustNow ? "You're on the latest version." : "";
        Download.Content = update is null ? null : $"Download {update.Version.ToString(3)}";
        Download.IsVisible = update is not null;
    }

    private void Save(GeneralSettings settings)
    {
        _settings = settings;
        _general.Save(settings);
    }

    private Control CategoryRow(AppCategoryRow app)
    {
        var name = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 12, 0) };
        name.Children.Add(new TextBlock { Text = app.AppName, FontWeight = FontWeight.Medium, TextTrimming = TextTrimming.CharacterEllipsis });
        name.Children.Add(new TextBlock
        {
            Text = app.IsBrowser
                ? $"Browser. Tabs matching a keyword keep their own category; this sets the rest (default {app.DefaultCategory})."
                : $"Default {app.DefaultCategory}",
            Classes = { "muted" },
            FontSize = 12,
        });

        var choices = new[] { ("Default", (Category?)null) }.Concat(Enum.GetValues<Category>().Select(c => (c.ToString(), (Category?)c)));
        var picker = Segmented($"category-{app.AppName}", choices, app.Override,
            async category => await _categories.SetCategoryAsync(app.AppName, category, CancellationToken.None));
        picker.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(picker, 2);
        Grid.SetColumn(name, 1);

        return new Grid
        {
            Margin = new Thickness(0, 6),
            ColumnDefinitions = new ColumnDefinitions("44,*,Auto"),
            Children =
            {
                new AppIconView { AppName = app.AppName, Category = app.Effective, HorizontalAlignment = HorizontalAlignment.Left },
                name,
                picker,
            },
        };
    }

    /// <summary>A row of pill buttons where exactly one is selected.</summary>
    private static Border Segmented<T>(string group, IEnumerable<(string Label, T Value)> choices, T selected, Action<T> onSelect)
    {
        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var (label, value) in choices)
        {
            var button = new RadioButton
            {
                Content = label,
                GroupName = group,
                Classes = { "segment" },
                IsChecked = EqualityComparer<T>.Default.Equals(value, selected),
            };
            button.IsCheckedChanged += (_, _) =>
            {
                if (button.IsChecked == true)
                    onSelect(value);
            };
            buttons.Children.Add(button);
        }
        return new Border { Classes = { "segmentGroup" }, Child = buttons };
    }
}
