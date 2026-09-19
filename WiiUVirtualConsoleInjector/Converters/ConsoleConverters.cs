using Avalonia.Data.Converters;
using PD.WiiU.VirtualConsole;
using WiiUVirtualConsoleInjector.Assets;
using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector.Converters;

/// <summary>
/// Binding converters that turn a <see cref="SourceConsole"/> or a <see cref="ConsoleTile"/> into its logo or label.
/// </summary>
public static class ConsoleConverters
{
    /// <summary>
    /// Console to the note under its tile, or null.
    /// </summary>
    public static readonly IValueConverter Caption = new FuncValueConverter<SourceConsole, string?>(ConsoleIcons.Caption);
    /// <summary>
    /// True when the console has a note under its tile.
    /// </summary>
    public static readonly IValueConverter HasCaption = new FuncValueConverter<SourceConsole, bool>(c => ConsoleIcons.Caption(c) is not null);
    /// <summary>
    /// Console to its near-black logo bitmap, for light tiles.
    /// </summary>
    public static readonly IValueConverter DarkIcon = new FuncValueConverter<SourceConsole, object?>(c => ConsoleIcons.DarkFor(c));
    /// <summary>
    /// Console to its white logo bitmap.
    /// </summary>
    public static readonly IValueConverter Icon = new FuncValueConverter<SourceConsole, object?>(c => ConsoleIcons.For(c));
    /// <summary>
    /// Console to its display name.
    /// </summary>
    public static readonly IValueConverter Name = new FuncValueConverter<SourceConsole, string>(ConsoleIcons.DisplayName);
    /// <summary>
    /// Tile to its near-black logo bitmap, for light tiles.
    /// </summary>
    public static readonly IValueConverter TileDarkIcon = new FuncValueConverter<ConsoleTile?, object?>(t => t is null ? null : ConsoleIcons.DarkFor(t.IconName));
    /// <summary>
    /// Tile to its white logo bitmap.
    /// </summary>
    public static readonly IValueConverter TileIcon = new FuncValueConverter<ConsoleTile?, object?>(t => t is null ? null : ConsoleIcons.For(t.IconName));
}
