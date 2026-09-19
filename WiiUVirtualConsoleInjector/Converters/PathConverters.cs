using Avalonia.Data.Converters;

namespace WiiUVirtualConsoleInjector.Converters;

/// <summary>
/// Path-to-text converters.
/// </summary>
public static class PathConverters
{
    /// <summary>
    /// The file name of a path; empty for null.
    /// </summary>
    public static readonly IValueConverter FileName = new FuncValueConverter<string?, string>(path => path is null ? "" : Path.GetFileName(path));
}
