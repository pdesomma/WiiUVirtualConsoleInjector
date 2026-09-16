namespace PD.WiiU.VirtualConsole.Ports;

/// <summary>
/// Source of unpacked base titles.
/// </summary>
public interface IBaseStore
{
    /// <summary>
    /// Puts a base into the store from a folder on disk: a title's code/content/meta as is, or an installable package unpacked with the common key.
    /// </summary>
    /// <param name="base">Base the folder holds.</param>
    /// <param name="sourceDirectory">The folder.</param>
    /// <param name="commonKey">Needed for a package; ignored for plain folders.</param>
    /// <param name="progress">One line per step.</param>
    /// <param name="cancellationToken">Cancels between files.</param>
    /// <exception cref="InvalidOperationException">A package with no common key.</exception>
    /// <exception cref="InvalidDataException">The folder is neither a title nor a package, or the package does not verify.</exception>
    Task<TitleDirectory> ImportAsync(BaseTitle @base, string sourceDirectory, WiiUSharp.Nus.CommonKey? commonKey, IProgress<string>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Where the base lives in the store; may not exist.
    /// </summary>
    /// <param name="base">Base to find.</param>
    TitleDirectory Locate(BaseTitle @base);

    /// <summary>
    /// Copies a base into a working folder.
    /// </summary>
    /// <param name="base">Base to stage.</param>
    /// <param name="destination">Folder that becomes the title root.</param>
    /// <param name="cancellationToken">Cancels the copy.</param>
    Task<TitleDirectory> StageAsync(BaseTitle @base, string destination, CancellationToken cancellationToken = default);
}
