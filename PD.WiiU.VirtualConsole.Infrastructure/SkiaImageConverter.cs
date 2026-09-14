using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp;
using WiiUSharp.Imaging;

namespace PD.WiiU.VirtualConsole.Infrastructure;

/// <summary>
/// Artwork conversion via WiiUSharp.Imaging.
/// </summary>
public sealed class SkiaImageConverter : IImageConverter
{
    /// <inheritdoc/>
    public Task ConvertAsync(string sourcePath, ImageSlot slot, string destinationPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TitleImage.Convert(sourcePath, slot, destinationPath);
        return Task.CompletedTask;
    }
}
