namespace PD.WiiU.VirtualConsole.Ports;

/// <summary>
/// Packs an unpacked title into an installable one.
/// </summary>
public interface ITitlePacker
{
    /// <summary>
    /// Writes the installable title to the output folder.
    /// </summary>
    /// <param name="title">Unpacked title.</param>
    /// <param name="outputDirectory">Folder to write into.</param>
    /// <param name="progress">Receives step detail.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    Task PackAsync(TitleDirectory title, string outputDirectory, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
}
