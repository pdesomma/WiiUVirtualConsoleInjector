using WiiUSharp.Nus;

namespace PD.WiiU.VirtualConsole.Ports;

/// <summary>
/// Fetches a base from the update server and unpacks it into the store.
/// </summary>
public interface IBaseDownloader
{
    /// <summary>
    /// Downloads and unpacks; the base is in the store when this returns.
    /// </summary>
    /// <param name="base">Base to fetch.</param>
    /// <param name="titleKey">The user's wrapped title key for it.</param>
    /// <param name="commonKey">The user's common key.</param>
    /// <param name="progress">Per-file byte counts, then per-content unpacking.</param>
    /// <param name="cancellationToken">Stops the transfer.</param>
    /// <exception cref="InvalidDataException">The keys do not unlock the title.</exception>
    Task<TitleDirectory> DownloadAsync(BaseTitle @base, EncryptedTitleKey titleKey, CommonKey commonKey, IProgress<BaseDownloadProgress>? progress = null, CancellationToken cancellationToken = default);
}
