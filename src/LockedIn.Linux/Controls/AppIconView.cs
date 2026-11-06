using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using LockedIn.Data;
using LockedIn.Linux.Theming;

namespace LockedIn.Linux.Controls;

/// <summary>An app's first letter on its category colour.</summary>
public sealed class AppIconView : Border
{
    public static readonly StyledProperty<string?> AppNameProperty = AvaloniaProperty.Register<AppIconView, string?>(nameof(AppName));

    public static readonly StyledProperty<Category> CategoryProperty =
        AvaloniaProperty.Register<AppIconView, Category>(nameof(Category), Category.Neutral);

    public AppIconView()
    {
        Width = Height = 32;
        CornerRadius = new CornerRadius(8);
    }

    public string? AppName
    {
        get => GetValue(AppNameProperty);
        set => SetValue(AppNameProperty, value);
    }

    public Category Category
    {
        get => GetValue(CategoryProperty);
        set => SetValue(CategoryProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == AppNameProperty || change.Property == CategoryProperty)
            Update();
    }

    private void Update()
    {
        if (string.IsNullOrEmpty(AppName))
            return;

        Background = ThemeManager.For(Category);
        Child = new TextBlock
        {
            Text = AppName[..1].ToUpperInvariant(),
            Foreground = Brushes.White,
            FontWeight = FontWeight.SemiBold,
            FontSize = 14,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
    }
}
