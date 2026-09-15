using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace WiiUVirtualConsoleInjector.Converters;

/// <summary>
/// Turns a file path on disk into a bitmap for previews.
/// </summary>
public static class FileImageConverter
{
    private const int CacheLimit = 32;

    private static readonly Dictionary<string, Bitmap?> Cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// File path to bitmap; null when blank, missing or not decodable.
    /// </summary>
    public static readonly IValueConverter Bitmap = new FuncValueConverter<string?, object?>(Load);

    /// <summary>
    /// File path to true when no preview can be shown for it.
    /// </summary>
    public static readonly IValueConverter IsMissing = new FuncValueConverter<string?, bool>(p => Load(p) is null);

    /// <summary>
    /// Decodes the file once, swallowing every failure so a bad preview never breaks the page.
    /// </summary>
    /// <param name="path">File to read.</param>
    internal static Bitmap? Load(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return null;

        lock (Cache)
        {
            if (Cache.TryGetValue(path!, out var cached))
                return cached;

            if (Cache.Count >= CacheLimit)
                Cache.Clear();

            var bitmap = Decode(path!);
            Cache[path!] = bitmap;
            return bitmap;
        }
    }

    /// <summary>
    /// Reads the bitmap, or null when the platform or codec refuses it.
    /// </summary>
    /// <param name="path">File to read.</param>
    private static Bitmap? Decode(string path)
    {
        try
        {
            return new Bitmap(path);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
