namespace PD.WiiU.VirtualConsole.Options;

/// <summary>
/// How the GamePad presents itself to a Wii game.
/// </summary>
public enum WiiControllerMode
{
    /// <summary>
    /// Classic Controller.
    /// </summary>
    ClassicController,
    /// <summary>
    /// Sideways Wii Remote.
    /// </summary>
    HorizontalWiiRemote,
    /// <summary>
    /// Upright Wii Remote.
    /// </summary>
    WiiRemote,
    /// <summary>
    /// Classic Controller from boot, skipping the sync prompt.
    /// </summary>
    InstantClassicController,
    /// <summary>
    /// No Classic Controller emulation.
    /// </summary>
    NoClassicController,
}
