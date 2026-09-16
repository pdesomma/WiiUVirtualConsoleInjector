namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Save memory types the N64 Virtual Console can emulate, numbered as its INI expects.
/// </summary>
public enum N64BackupType
{
    /// <summary>
    /// Let the emulator detect it.
    /// </summary>
    Auto = 0,
    /// <summary>
    /// Battery-backed SRAM.
    /// </summary>
    Sram = 1,
    /// <summary>
    /// Flash RAM.
    /// </summary>
    Flash = 2,
    /// <summary>
    /// Serial EEPROM.
    /// </summary>
    Eeprom = 3,
}
