using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LockedIn.App.Views;

/// <summary>The bundled illustrations. Decoded at display size each time a page needs them, so closing the window frees them.</summary>
internal static class Art
{
    public static ImageSource Stage(int level, int width) => Load($"stage-{level}.png", width);

    public static ImageSource EmptyState() => Load("empty-state.png", 640);

    private static BitmapImage Load(string file, int decodeWidth)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri($"pack://application:,,,/Assets/{file}");
        image.DecodePixelWidth = decodeWidth;
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
