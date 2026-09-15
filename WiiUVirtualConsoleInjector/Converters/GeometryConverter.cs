using Avalonia.Data.Converters;
using Avalonia.Media;

namespace WiiUVirtualConsoleInjector.Converters;

/// <summary>
/// Parses SVG path data into a geometry for <see cref="Avalonia.Controls.Shapes.Path"/>.
/// </summary>
public static class GeometryConverter
{
    /// <summary>
    /// Path data string to geometry; null for a blank string.
    /// </summary>
    public static readonly IValueConverter Parse = new FuncValueConverter<string?, Geometry?>(data => string.IsNullOrWhiteSpace(data) ? null : Geometry.Parse(data));
}
