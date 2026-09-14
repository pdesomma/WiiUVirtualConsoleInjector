namespace PD.WiiU.VirtualConsole.Ports;

/// <summary>
/// Turns an audio file into bootSound.btsnd.
/// </summary>
public interface IBootSoundConverter
{
    /// <summary>
    /// Converts a source file to the boot sound format.
    /// </summary>
    /// <param name="sourcePath">Audio file.</param>
    /// <param name="destinationPath">Output .btsnd path.</param>
    /// <param name="cancellationToken">Cancels the conversion.</param>
    Task ConvertAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default);
}
