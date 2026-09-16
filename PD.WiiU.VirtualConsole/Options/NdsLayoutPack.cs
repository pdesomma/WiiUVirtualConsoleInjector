namespace PD.WiiU.VirtualConsole.Options;

/// <summary>
/// The bundled sets of extra DS screen layouts.
/// </summary>
public enum NdsLayoutPack
{
    /// <summary>
    /// Keep the base's own layouts.
    /// </summary>
    None,
    /// <summary>
    /// The full set, for any game.
    /// </summary>
    All,
    /// <summary>
    /// The set tuned for Phantom Hourglass and other games that want the touch screen on the TV.
    /// </summary>
    PhantomHourglass,
}
