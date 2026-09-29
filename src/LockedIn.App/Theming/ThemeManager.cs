using System.Windows;
using System.Windows.Media;
using LockedIn.Data;
using Microsoft.Extensions.Options;
using Microsoft.Win32;

namespace LockedIn.App.Theming;

/// <summary>
/// Puts the palette for the chosen theme and accent into the app's resources. Every view uses them through
/// DynamicResource, so switching theme, accent or Windows' own light/dark mode repaints everything live.
/// </summary>
internal static class ThemeManager
{
    private sealed record Palette(
        string Bg, string Card, string Raised, string Text, string Muted, string Border,
        string Focus, string Neutral, string Distraction);

    private static readonly Palette Light = new("#F5F5F7", "#FFFFFF", "#1F767680", "#1D1D1F", "#6E6E73", "#14000000", "#34C759", "#AEAEB2", "#FF3B30");
    private static readonly Palette Dark = new("#000000", "#1C1C1E", "#3D767680", "#F5F5F7", "#98989D", "#1AFFFFFF", "#30D158", "#636366", "#FF453A");

    private static IOptionsMonitor<AppearanceOptions>? _options;
    private static (bool Dark, Accent Accent)? _applied;

    /// <summary>Raised on the UI thread after the palette changed.</summary>
    public static event Action? Changed;

    public static bool IsDark { get; private set; }

    public static bool Animations => _options?.CurrentValue.Animations ?? true;

    public static void Initialize(IOptionsMonitor<AppearanceOptions> options)
    {
        _options = options;
        Apply();
        options.OnChange(_ => Application.Current.Dispatcher.BeginInvoke(Apply));
        SystemEvents.UserPreferenceChanged += (_, e) =>
        {
            if (e.Category == UserPreferenceCategory.General)
                Application.Current.Dispatcher.BeginInvoke(Apply);
        };
    }

    public static Brush Brush(string key) => (Brush)Application.Current.Resources[key];

    public static Brush For(Category category) => Brush(category.ToString());

    private static void Apply()
    {
        var look = _options!.CurrentValue;
        IsDark = look.Theme switch
        {
            Theme.Light => false,
            Theme.Dark => true,
            _ => Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1) is 0,
        };

        // Windows raises preference events for many unrelated reasons; only repaint when the palette really changes.
        if (_applied == (IsDark, look.Accent))
            return;
        _applied = (IsDark, look.Accent);

        var palette = IsDark ? Dark : Light;
        var (accent, onAccent) = (look.Accent, IsDark) switch
        {
            (Accent.Graphite, false) => ("#1D1D1F", "#FFFFFF"),
            (Accent.Graphite, true) => ("#F5F5F7", "#1D1D1F"),
            (Accent.Orange, false) => ("#F56300", "#FFFFFF"),
            (Accent.Orange, true) => ("#FF9F0A", "#FFFFFF"),
            (_, false) => ("#0071E3", "#FFFFFF"),
            (_, true) => ("#0A84FF", "#FFFFFF"),
        };

        var resources = Application.Current.Resources;
        Set(resources, "Bg", palette.Bg);
        Set(resources, "Card", palette.Card);
        Set(resources, "Raised", palette.Raised);
        Set(resources, "Text", palette.Text);
        Set(resources, "Muted", palette.Muted);
        Set(resources, "Border", palette.Border);
        Set(resources, nameof(Category.Focus), palette.Focus);
        Set(resources, nameof(Category.Neutral), palette.Neutral);
        Set(resources, nameof(Category.Distraction), palette.Distraction);
        Set(resources, "Accent", accent);
        Set(resources, "OnAccent", onAccent);
        Set(resources, "AccentSoft", "#26" + accent[1..]);
        Changed?.Invoke();
    }

    private static void Set(ResourceDictionary resources, string key, string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze(); // frozen brushes are cheaper to draw and never change behind a view's back
        resources[key] = brush;
    }
}
