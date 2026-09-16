using Avalonia.Data.Converters;
using PD.WiiU.VirtualConsole.Wii;

namespace WiiUVirtualConsoleInjector.Converters;

/// <summary>
/// Labels for the Nintendont choices.
/// </summary>
public static class NintendontConverters
{
    /// <summary>
    /// A forced mode as the menu shows it.
    /// </summary>
    public static readonly IValueConverter ForcedModeLabel = new FuncValueConverter<object?, string>(value => value switch
    {
        NintendontForcedMode.Ntsc => "NTSC",
        NintendontForcedMode.Pal60 => "PAL60",
        NintendontForcedMode.Pal50 => "PAL50",
        NintendontForcedMode.MPal => "MPAL",
        _ => "",
    });

    /// <summary>
    /// A memory card size index as its block count.
    /// </summary>
    public static readonly IValueConverter MemoryCardLabel = new FuncValueConverter<object?, string>(value => value is int i ? $"{NintendontConfig.MemoryCardBlocks(i)} blocks" : "");

    /// <summary>
    /// A video setting as the menu shows it.
    /// </summary>
    public static readonly IValueConverter VideoLabel = new FuncValueConverter<object?, string>(value => value switch
    {
        NintendontVideo.Auto => "Auto",
        NintendontVideo.Force => "Force",
        NintendontVideo.ForceDeflicker => "Force, deflicker",
        NintendontVideo.None => "Leave alone",
        _ => "",
    });
}
