using Avalonia.Data.Converters;

namespace WiiUVirtualConsoleInjector.Converters;

/// <summary>
/// Small boolean-to-visual converters.
/// </summary>
public static class BoolConverters
{
    /// <summary>
    /// True to full opacity, false to dimmed.
    /// </summary>
    public static readonly IValueConverter OpacityOn = new FuncValueConverter<bool, double>(on => on ? 1.0 : 0.45);
}
