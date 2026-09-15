using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Infrastructure;

namespace WiiUVirtualConsoleInjector.Converters;

/// <summary>
/// Converters that show overlay art in the frame pickers.
/// </summary>
public static class OverlayConverters
{
    private static readonly Dictionary<string, Bitmap?> Cache = new(StringComparer.Ordinal);

    /// <summary>
    /// The overlay's own art as a bitmap; null for None or an unknown file.
    /// </summary>
    public static readonly IValueConverter Thumbnail = new FuncValueConverter<ArtworkFrame?, object?>(Load);

    /// <summary>
    /// Reads the overlay's embedded PNG, once.
    /// </summary>
    /// <param name="frame">Frame to show, or null.</param>
    internal static Bitmap? Load(ArtworkFrame? frame)
    {
        if (frame?.Resource is not { } resource)
            return null;

        lock (Cache)
        {
            if (Cache.TryGetValue(resource, out var cached))
                return cached;

            using var stream = typeof(SkiaArtworkComposer).Assembly.GetManifestResourceStream(SkiaArtworkComposer.ResourcePrefix + resource);
            var bitmap = stream is null ? null : new Bitmap(stream);
            Cache[resource] = bitmap;
            return bitmap;
        }
    }
}
