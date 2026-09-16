namespace PD.WiiU.VirtualConsole.Wii;

/// <summary>
/// How Nintendont picks the video mode, numbered as its configuration stores them.
/// </summary>
public enum NintendontVideo
{
    /// <summary>
    /// Follow the game and the console.
    /// </summary>
    Auto = 0,
    /// <summary>
    /// Force the chosen mode.
    /// </summary>
    Force = 1,
    /// <summary>
    /// Leave the mode alone entirely.
    /// </summary>
    None = 2,
    /// <summary>
    /// Force the chosen mode and keep the deflicker filter on.
    /// </summary>
    ForceDeflicker = 4,
}
