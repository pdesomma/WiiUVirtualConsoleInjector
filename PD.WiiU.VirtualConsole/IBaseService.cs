namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Gets bases into the store: what each one needs, and downloading it with the user's keys.
/// </summary>
public interface IBaseService
{
    /// <summary>
    /// The bases known for a console.
    /// </summary>
    /// <param name="console">Console the ROM is for.</param>
    IReadOnlyList<BaseTitle> Available(SourceConsole console);

    /// <summary>
    /// Downloads a base with the keys in the store.
    /// </summary>
    /// <param name="base">Base to fetch.</param>
    /// <param name="progress">Per-file, then per-content.</param>
    /// <param name="cancellationToken">Stops the transfer.</param>
    /// <exception cref="InvalidOperationException">A key is missing; see <see cref="Status"/>.</exception>
    /// <exception cref="InvalidDataException">The keys do not unlock the title.</exception>
    Task<TitleDirectory> DownloadAsync(BaseTitle @base, IProgress<BaseDownloadProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the base is present, or which key it still needs.
    /// </summary>
    /// <param name="base">Base to check.</param>
    BaseStatus Status(BaseTitle @base);
}
