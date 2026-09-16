namespace PD.WiiU.VirtualConsole.Wii;

/// <summary>
/// Modes Nintendont can force, numbered by the bit each sets.
/// </summary>
public enum NintendontForcedMode
{
    /// <summary>
    /// 576i at 50 Hz.
    /// </summary>
    Pal50 = 0,
    /// <summary>
    /// 480i at 60 Hz, PAL colour.
    /// </summary>
    Pal60 = 1,
    /// <summary>
    /// 480i at 60 Hz.
    /// </summary>
    Ntsc = 2,
    /// <summary>
    /// Brazilian PAL-M.
    /// </summary>
    MPal = 3,
}
