using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp.Nus;
using WiiUSharp.Wud;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Reads .wud and .wux dumps; each title lands in a folder named by the product code and title ID.
/// </summary>
public sealed class WudDiscBackup : IDiscBackup
{
    private static readonly string[] Extensions = { ".wud", ".wux" };

    /// <inheritdoc/>
    public bool Accepts(string imagePath) => !string.IsNullOrWhiteSpace(imagePath) && Extensions.Contains(Path.GetExtension(imagePath), StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> UnpackAsync(string imagePath, DiscKey? discKey, CommonKey commonKey, string outputFolder, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            throw new ArgumentException("Image path is required.", nameof(imagePath));
        if (string.IsNullOrWhiteSpace(outputFolder))
            throw new ArgumentException("Output folder is required.", nameof(outputFolder));
        if (!File.Exists(imagePath))
            throw new FileNotFoundException("The disc image is not there.", imagePath);

        return Task.Run(() =>
        {
            var key = discKey ?? DiscKey.Beside(imagePath) ?? throw new FileNotFoundException($"No {WudFormat.DiscKeyFileName} beside the image; enter the disc key.", Path.Combine(Path.GetDirectoryName(imagePath) ?? "", WudFormat.DiscKeyFileName));
            using var image = WudImage.Open(imagePath);
            var disc = WudDisc.Read(image, key);
            var written = new List<string>();
            foreach (var title in disc.Titles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var folder = Path.Combine(outputFolder, $"{disc.ProductCode} [{title.TitleId}]");
                progress?.Report($"{title.TitleId} to {folder}");
                title.WritePackage(image, commonKey, folder, progress, cancellationToken);
                written.Add(folder);
            }
            return (IReadOnlyList<string>)written;
        }, cancellationToken);
    }
}
