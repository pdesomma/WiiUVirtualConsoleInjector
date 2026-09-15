using Avalonia.Data.Converters;
using PD.WiiU.VirtualConsole;

namespace WiiUVirtualConsoleInjector.Converters;

/// <summary>
/// Converters for the output format choice.
/// </summary>
public static class FormatConverters
{
    /// <summary>
    /// Format to the name shown in the picker.
    /// </summary>
    public static readonly IValueConverter Name = new FuncValueConverter<OutputFormat, string>(NameFor);

    /// <summary>
    /// The name shown for a format.
    /// </summary>
    /// <param name="format">Format to name.</param>
    public static string NameFor(OutputFormat format) => format == OutputFormat.Loadiine ? "Loadiine" : "WUP installer";
}
