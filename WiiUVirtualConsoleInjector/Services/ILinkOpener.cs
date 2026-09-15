namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// Opens a web address in the user's browser.
/// </summary>
public interface ILinkOpener
{
    /// <summary>
    /// Hands the address to the system browser; false when nothing could take it.
    /// </summary>
    /// <param name="uri">Absolute address.</param>
    Task<bool> OpenAsync(Uri uri);
}
