using Avalonia.Data.Converters;
using PD.WiiU.VirtualConsole;

namespace WiiUVirtualConsoleInjector.Converters;

/// <summary>
/// Labels for the N64 INI choices.
/// </summary>
public static class N64IniConverters
{
    /// <summary>
    /// A save memory type as the menu shows it; null is detection.
    /// </summary>
    public static readonly IValueConverter BackupTypeLabel = new FuncValueConverter<object?, string>(value => value switch
    {
        N64BackupType.Eeprom => "EEPROM",
        N64BackupType.Sram => "SRAM",
        N64BackupType.Flash => "Flash RAM",
        N64BackupType.Auto => "Auto (written as 0)",
        _ => "Not set",
    });
}
