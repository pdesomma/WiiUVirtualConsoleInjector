namespace PD.WiiU.VirtualConsole.Ports;

/// <summary>
/// The bundled RetroArch cores and the title template they run in.
/// </summary>
public interface IRetroArchCores
{
    /// <summary>
    /// The cores shipped for a console, recommended first; empty when the console has none.
    /// </summary>
    /// <param name="console">Console the ROM is for.</param>
    IReadOnlyList<RetroArchCore> Available(SourceConsole console);

    /// <summary>
    /// Writes the template title with the core as its executable into a working folder.
    /// </summary>
    /// <param name="core">Core to stage.</param>
    /// <param name="destination">Folder that becomes the title root.</param>
    /// <param name="cancellationToken">Cancels the copy.</param>
    Task<TitleDirectory> StageAsync(RetroArchCore core, string destination, CancellationToken cancellationToken = default);
}
