namespace PD.WiiU.VirtualConsole.Wii;

/// <summary>
/// Changes that can be made to the vWii IOS image (code/fw.img).
/// </summary>
public enum FirmwarePatch
{
    /// <summary>
    /// Skip signature checks so modified content loads.
    /// </summary>
    FakeSign,
    /// <summary>
    /// Map Classic Controller L/R to GamePad ZL/ZR.
    /// </summary>
    ShoulderToTrigger,
    /// <summary>
    /// Report a Wii Remote instead of a Classic Controller.
    /// </summary>
    WiiRemote,
    /// <summary>
    /// Remap d-pad and face buttons for a sideways Wii Remote; needs <see cref="WiiRemote"/>.
    /// </summary>
    HorizontalWiiRemote,
    /// <summary>
    /// Disable AHBPROT and MEMPROT and apply the Nintendont hooks.
    /// </summary>
    Homebrew,
    /// <summary>
    /// Pass real Wii Remotes through to homebrew.
    /// </summary>
    Passthrough,
    /// <summary>
    /// Report the Classic Controller immediately.
    /// </summary>
    InstantClassicController,
    /// <summary>
    /// Report no Classic Controller connected.
    /// </summary>
    NoClassicController,
}
