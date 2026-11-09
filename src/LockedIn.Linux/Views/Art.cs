using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace LockedIn.Linux.Views;

/// <summary>The bundled illustrations. Decoded at display size each time a page needs them, so closing the window frees them.</summary>
internal static class Art
{
    public static Bitmap Stage(int level, int width) => Load($"stage-{level}.png", width);

    public static Bitmap EmptyState() => Load("empty-state.png", 640);

    private static Bitmap Load(string file, int width)
    {
        using var stream = AssetLoader.Open(new Uri($"avares://locked-in/Assets/{file}"));
        return Bitmap.DecodeToWidth(stream, width);
    }
}
