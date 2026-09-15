namespace PD.WiiU.VirtualConsole;

/// <summary>
/// The shape an injected title is written in.
/// </summary>
public enum OutputFormat
{
    /// <summary>
    /// Encrypted and signed for an installer; goes in the card's install folder.
    /// </summary>
    Wup,
    /// <summary>
    /// The plain code, content and meta folders for Loadiine; goes in the card's wiiu/games folder.
    /// </summary>
    Loadiine,
}
