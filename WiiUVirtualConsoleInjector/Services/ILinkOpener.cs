namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// Hands addresses and folders to the user's shell.
/// </summary>
public interface ILinkOpener
{
    /// <summary>
    /// Hands the address to the system browser; false when nothing could take it.
    /// </summary>
    /// <param name="uri">Absolute address.</param>
    Task<bool> OpenAsync(Uri uri);

    /// <summary>
    /// Shows a folder in the file manager, creating it when it is not there yet.
    /// </summary>
    /// <param name="path">Folder to show.</param>
    Task<bool> OpenFolderAsync(string path);
}
