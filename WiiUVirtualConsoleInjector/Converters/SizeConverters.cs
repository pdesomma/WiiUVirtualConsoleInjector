using Avalonia.Data.Converters;
using Avalonia.Media;
using PD.WiiU.VirtualConsole;

namespace WiiUVirtualConsoleInjector.Converters;

/// <summary>
/// Sizes as the console's data management shows them: blue in KB, orange in MB, red in GB.
/// </summary>
public static class SizeConverters
{
    private static readonly IBrush Gb = new SolidColorBrush(Color.Parse("#E53935"));
    private static readonly IBrush Kb = new SolidColorBrush(Color.Parse("#2F80ED"));
    private static readonly IBrush Mb = new SolidColorBrush(Color.Parse("#F2994A"));

    /// <summary>
    /// A <see cref="ByteSize"/> or byte count to the brush for its unit; null for anything else.
    /// </summary>
    public static readonly IValueConverter Brush = new FuncValueConverter<object?, IBrush?>(value => Of(value) is { } size ? size.Unit switch
    {
        ByteSizeUnit.Kilobytes => Kb,
        ByteSizeUnit.Megabytes => Mb,
        _ => Gb,
    } : null);

    /// <summary>
    /// A <see cref="ByteSize"/> or byte count to its text; empty for anything else.
    /// </summary>
    public static readonly IValueConverter Text = new FuncValueConverter<object?, string>(value => Of(value)?.Text ?? "");

    private static ByteSize? Of(object? value) => value switch
    {
        ByteSize size => size,
        long bytes => new ByteSize(bytes),
        int bytes => new ByteSize(bytes),
        _ => null,
    };
}
