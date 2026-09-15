using WiiUSharp;
using WiiUSharp.Nus;

namespace PD.WiiU.VirtualConsole.Ports;

/// <summary>
/// Where the user's keys live. Nothing here ships with the application.
/// </summary>
public interface IKeyStore
{
    /// <summary>
    /// The console's ancast key, once the user has supplied it; only Wii homebrew overclocking needs it.
    /// </summary>
    AncastKey? AncastKey { get; set; }

    /// <summary>
    /// The Wii U common key, once the user has supplied it.
    /// </summary>
    CommonKey? CommonKey { get; set; }

    /// <summary>
    /// The wrapped title key for a title, or null when the user has not supplied one.
    /// </summary>
    /// <param name="titleId">Title the key unlocks.</param>
    EncryptedTitleKey? GetTitleKey(TitleId titleId);

    /// <summary>
    /// Stores or clears a title key.
    /// </summary>
    /// <param name="titleId">Title the key unlocks.</param>
    /// <param name="titleKey">Key to keep, or null to forget it.</param>
    void SetTitleKey(TitleId titleId, EncryptedTitleKey? titleKey);
}
