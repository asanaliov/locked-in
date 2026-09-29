using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LockedIn.App.Theming;
using LockedIn.Data;

namespace LockedIn.App.Controls;

/// <summary>An app's real icon when one was saved, otherwise its first letter on the category colour.</summary>
public sealed class AppIconView : Border
{
    // Icons are tiny and shown on every page, so each is decoded once per run.
    private static readonly Dictionary<string, ImageSource?> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static readonly DependencyProperty AppNameProperty = DependencyProperty.Register(
        nameof(AppName), typeof(string), typeof(AppIconView), new PropertyMetadata(null, (d, _) => ((AppIconView)d).Update()));

    public static readonly DependencyProperty CategoryProperty = DependencyProperty.Register(
        nameof(Category), typeof(Category), typeof(AppIconView), new PropertyMetadata(Category.Neutral, (d, _) => ((AppIconView)d).Update()));

    public AppIconView()
    {
        Width = Height = 32;
        CornerRadius = new CornerRadius(8);
    }

    public string? AppName
    {
        get => (string?)GetValue(AppNameProperty);
        set => SetValue(AppNameProperty, value);
    }

    public Category Category
    {
        get => (Category)GetValue(CategoryProperty);
        set => SetValue(CategoryProperty, value);
    }

    private void Update()
    {
        if (string.IsNullOrEmpty(AppName))
            return;

        if (Load(AppName) is { } icon)
        {
            Background = null;
            Child = new Image { Source = icon, Stretch = Stretch.Uniform };
            return;
        }

        Background = ThemeManager.For(Category);
        Child = new TextBlock
        {
            Text = AppName[..1].ToUpperInvariant(),
            Foreground = Brushes.White,
            FontWeight = FontWeights.SemiBold,
            FontSize = Width * 0.45,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
    }

    private static ImageSource? Load(string appName)
    {
        if (Cache.TryGetValue(appName, out var cached))
            return cached;

        ImageSource? icon = null;
        if (AppIconFiles.Exists(appName))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(AppIconFiles.PathFor(appName));
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // don't keep the file open
                bitmap.EndInit();
                bitmap.Freeze();
                icon = bitmap;
            }
            catch (Exception ex) when (ex is IOException or NotSupportedException)
            {
                // A broken icon file just falls back to the letter.
            }
        }
        return Cache[appName] = icon;
    }
}
