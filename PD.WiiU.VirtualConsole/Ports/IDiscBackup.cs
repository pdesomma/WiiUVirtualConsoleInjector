using WiiUSharp.Nus;
using WiiUSharp.Wud;

namespace PD.WiiU.VirtualConsole.Ports;

/// <summary>
/// Turns a Wii U disc dump into installable packages.
/// </summary>
public interface IDiscBackup
{
    /// <summary>
    /// True when the path is a dump this reads: .wud, .wux, or a part of a split .wud.
    /// </summary>
    /// <param name="imagePath">File to look at.</param>
    bool Accepts(string imagePath);

    /// <summary>
    /// Writes every title on the disc as a package folder under the output folder and returns those folders.
    /// </summary>
    /// <param name="imagePath">The dump.</param>
    /// <param name="discKey">The disc's key; null to use the game.key beside the image.</param>
    /// <param name="commonKey">Unlocks the title keys.</param>
    /// <param name="outputFolder">Where package folders go.</param>
    /// <param name="progress">One line per file.</param>
    /// <param name="cancellationToken">Cancels between files.</param>
    /// <exception cref="FileNotFoundException">No image, or no key beside it.</exception>
    /// <exception cref="InvalidDataException">Not a dump, or a key is wrong.</exception>
    Task<IReadOnlyList<string>> UnpackAsync(string imagePath, DiscKey? discKey, CommonKey commonKey, string outputFolder, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
}
