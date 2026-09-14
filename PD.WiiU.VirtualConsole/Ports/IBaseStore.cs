namespace PD.WiiU.VirtualConsole.Ports;

/// <summary>
/// Source of unpacked base titles.
/// </summary>
public interface IBaseStore
{
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
