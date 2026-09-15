using WiiUSharp;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// One slot's image, freshly built.
/// </summary>
public sealed class ArtworkBuiltEventArgs : EventArgs
{
    /// <summary>
    /// Creates a new instance of the <see cref="ArtworkBuiltEventArgs"/> class.
    /// </summary>
    /// <param name="slot">Slot that was built.</param>
    /// <param name="path">PNG it was built into.</param>
    public ArtworkBuiltEventArgs(ImageSlot slot, string path)
    {
        Slot = slot ?? throw new ArgumentNullException(nameof(slot));
        Path = path ?? throw new ArgumentNullException(nameof(path));
    }

    /// <summary>
    /// PNG it was built into.
    /// </summary>
    public string Path { get; }
    /// <summary>
    /// Slot that was built.
    /// </summary>
    public ImageSlot Slot { get; }
}
