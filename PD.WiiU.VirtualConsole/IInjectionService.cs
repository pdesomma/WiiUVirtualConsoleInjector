namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Turns an <see cref="Injection"/> into an installable title.
/// </summary>
public interface IInjectionService
{
    /// <summary>
    /// Runs every step and packs the result.
    /// </summary>
    /// <param name="injection">What to build.</param>
    /// <param name="workDirectory">Folder for temporary files.</param>
    /// <param name="outputDirectory">Folder for the packed title.</param>
    /// <param name="progress">Receives step changes.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    /// <exception cref="InjectionException">A step failed.</exception>
    Task<InjectedTitle> InjectAsync(Injection injection, string workDirectory, string outputDirectory, IProgress<InjectionProgress>? progress = null, CancellationToken cancellationToken = default);
}
