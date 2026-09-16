namespace PD.WiiU.VirtualConsole.Ports;

/// <summary>
/// The community's artwork repository, keyed by the ids <see cref="CommunityArtworkIds"/> derives.
/// </summary>
public interface ICommunityArtwork
{
    /// <summary>
    /// Downloads one file of a hit.
    /// </summary>
    /// <param name="source">File in the repository.</param>
    /// <param name="destinationPath">Where to put it; overwritten.</param>
    /// <param name="cancellationToken">Cancels the download.</param>
    Task DownloadAsync(Uri source, string destinationPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// The first id that has artwork, or null when none does.
    /// </summary>
    /// <param name="ids">Repository paths to try, in order.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    Task<CommunityArtworkHit?> FindAsync(IReadOnlyList<string> ids, CancellationToken cancellationToken = default);
}
