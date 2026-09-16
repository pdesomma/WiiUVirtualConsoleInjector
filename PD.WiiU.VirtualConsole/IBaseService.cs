namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Gets bases into the store: what each one needs, and downloading it with the user's keys.
/// </summary>
public interface IBaseService
{
    /// <summary>
    /// Remembers a base the catalog does not list; replaces one with the same title ID.
    /// </summary>
    /// <param name="base">The base.</param>
    void AddCustom(BaseTitle @base);

    /// <summary>
    /// The bases known for a console: the catalog's, then the user's own.
    /// </summary>
    /// <param name="console">Console the ROM is for.</param>
    IReadOnlyList<BaseTitle> Available(SourceConsole console);

    /// <summary>
    /// Puts a base into the store from a folder: a title's code/content/meta as is, or an installable package unpacked with the common key.
    /// </summary>
    /// <param name="base">Base the folder holds.</param>
    /// <param name="sourceDirectory">The folder.</param>
    /// <param name="progress">One line per step.</param>
    /// <param name="cancellationToken">Cancels between files.</param>
    /// <exception cref="InvalidOperationException">A package with no common key in the store.</exception>
    Task<TitleDirectory> ImportAsync(BaseTitle @base, string sourceDirectory, IProgress<string>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Forgets a custom base; the store keeps its files.
    /// </summary>
    /// <param name="titleId">Its title ID.</param>
    /// <returns>False when nothing was remembered under it.</returns>
    bool RemoveCustom(WiiUSharp.TitleId titleId);

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
    /// True when the base's title key has been supplied.
    /// </summary>
    /// <param name="base">Base to check.</param>
    bool HasTitleKey(BaseTitle @base);

    /// <summary>
    /// Whether the base is present, or which key it still needs.
    /// </summary>
    /// <param name="base">Base to check.</param>
    BaseStatus Status(BaseTitle @base);
}
