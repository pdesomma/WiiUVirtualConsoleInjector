namespace PD.WiiU.VirtualConsole.Ports;

/// <summary>
/// Finds the previous application's data on this machine and carries it into the key store and base store.
/// </summary>
public interface ILegacyImport
{
    /// <summary>
    /// Looks for the install; null when there is none or it holds nothing worth taking.
    /// </summary>
    LegacyInstall? Find();

    /// <summary>
    /// Takes what is not already present: the common key, title keys, then each base.
    /// </summary>
    /// <param name="install">What was found.</param>
    /// <param name="progress">One line per base copied.</param>
    /// <param name="cancellationToken">Stops between bases.</param>
    Task<LegacyImportReport> ImportAsync(LegacyInstall install, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
}
