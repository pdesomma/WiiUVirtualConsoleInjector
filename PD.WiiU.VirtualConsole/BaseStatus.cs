namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Whether a base can be used or what it still needs.
/// </summary>
public enum BaseStatus
{
    /// <summary>
    /// In the store; ready to inject.
    /// </summary>
    Present,
    /// <summary>
    /// The user has not supplied the common key.
    /// </summary>
    NeedsCommonKey,
    /// <summary>
    /// The user has not supplied this title's key.
    /// </summary>
    NeedsTitleKey,
    /// <summary>
    /// Keys are in hand; the base can be downloaded.
    /// </summary>
    Downloadable,
}
