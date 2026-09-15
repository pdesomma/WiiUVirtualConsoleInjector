using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Ports;

/// <summary>
/// Builds icons and boot screens by dressing a screenshot in a console frame.
/// </summary>
public interface IArtworkComposer
{
    /// <summary>
    /// Draws one slot's image and writes it as a PNG.
    /// </summary>
    /// <param name="request">What to draw.</param>
    /// <param name="slot">Slot the image is for; only the icon and the two boot screens are drawn.</param>
    /// <param name="destinationPath">Output .png path.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    /// <exception cref="NotSupportedException">A slot this composer does not draw.</exception>
    Task ComposeAsync(ArtworkRequest request, ImageSlot slot, string destinationPath, CancellationToken cancellationToken = default);
}
