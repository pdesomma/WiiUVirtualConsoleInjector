namespace PD.WiiU.VirtualConsole.Options;

/// <summary>
/// Video standard to force on every render mode the game defines.
/// </summary>
public enum WiiVideoMode
{
    /// <summary>
    /// Leave the game's modes alone.
    /// </summary>
    Unchanged,
    /// <summary>
    /// NTSC 60 Hz, 480 lines.
    /// </summary>
    Ntsc,
    /// <summary>
    /// PAL 50 Hz, 528 lines.
    /// </summary>
    Pal50,
    /// <summary>
    /// PAL 60 Hz (EURGB60), 480 lines.
    /// </summary>
    Pal60,
}
