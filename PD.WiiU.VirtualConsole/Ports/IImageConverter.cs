using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Ports;

/// <summary>
/// Turns an image file into the TGA a slot expects.
/// </summary>
public interface IImageConverter
{
    /// <summary>
    /// Converts a source image to fit the slot.
    /// </summary>
    /// <param name="sourcePath">Image file.</param>
    /// <param name="slot">Slot the image is for.</param>
    /// <param name="destinationPath">Output .tga path.</param>
    /// <param name="cancellationToken">Cancels the conversion.</param>
    Task ConvertAsync(string sourcePath, ImageSlot slot, string destinationPath, CancellationToken cancellationToken = default);
}
