namespace PD.WiiU.VirtualConsole.Ports;

/// <summary>
/// Where a copy of Nintendont comes from, for the SD card an injected GameCube title boots from.
/// </summary>
public interface INintendontSource
{
    /// <summary>
    /// Puts Nintendont's loader, banner and icon under apps/nintendont on the card, replacing what is there.
    /// </summary>
    /// <param name="cardRoot">The card's root.</param>
    /// <param name="progress">One line per file.</param>
    /// <param name="cancellationToken">Cancels the download.</param>
    Task InstallAsync(string cardRoot, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
}
