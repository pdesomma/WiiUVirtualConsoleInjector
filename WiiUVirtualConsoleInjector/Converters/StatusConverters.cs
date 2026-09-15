using Avalonia.Data.Converters;
using Avalonia.Media;

namespace WiiUVirtualConsoleInjector.Converters;

/// <summary>
/// Converters for the base list's status glyphs.
/// </summary>
public static class StatusConverters
{
    private static readonly IBrush Off = new SolidColorBrush(Color.Parse("#BEBEBC"));
    private static readonly IBrush On = new SolidColorBrush(Color.Parse("#00AACC"));

    /// <summary>
    /// True to cyan, false to the muted gray.
    /// </summary>
    public static readonly IValueConverter Glyph = new FuncValueConverter<bool, IBrush>(on => on ? On : Off);

    /// <summary>
    /// True to full opacity, false to half.
    /// </summary>
    public static readonly IValueConverter UsableOpacity = new FuncValueConverter<bool, double>(on => on ? 1.0 : 0.5);
}
