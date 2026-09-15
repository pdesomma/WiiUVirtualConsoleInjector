using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace WiiUVirtualConsoleInjector.Converters;

/// <summary>
/// Turns an asset path relative to the Assets folder into a cached bitmap.
/// </summary>
public static class AssetConverter
{
    private const string Root = "avares://WiiUVirtualConsoleInjector/Assets/";

    private static readonly Dictionary<string, Bitmap> Cache = new();

    /// <summary>
    /// Asset path to bitmap; null for a blank path.
    /// </summary>
    public static readonly IValueConverter Bitmap = new FuncValueConverter<string?, object?>(Load);

    private static Bitmap? Load(string? asset)
    {
        if (string.IsNullOrWhiteSpace(asset))
            return null;

        lock (Cache)
        {
            if (!Cache.TryGetValue(asset!, out var bitmap))
            {
                using var stream = AssetLoader.Open(new Uri(Root + asset));
                bitmap = new Bitmap(stream);
                Cache[asset!] = bitmap;
            }
            return bitmap;
        }
    }
}
