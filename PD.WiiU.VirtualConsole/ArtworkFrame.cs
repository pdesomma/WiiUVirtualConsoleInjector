using WiiUSharp;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// One piece of frame art for one image slot, and where the screenshot shows through it.
/// </summary>
public sealed class ArtworkFrame
{
    /// <summary>
    /// Creates a new instance of the <see cref="ArtworkFrame"/> class.
    /// </summary>
    /// <param name="key">Stable identifier.</param>
    /// <param name="name">Name shown to the user.</param>
    /// <param name="slot">Slot the art is sized for.</param>
    /// <param name="console">Console it belongs to, or null when it suits every console.</param>
    /// <param name="resource">Image file, or null for no art at all.</param>
    /// <param name="window">Where the screenshot goes, or null when the slot shows none.</param>
    public ArtworkFrame(string key, string name, ImageSlot slot, SourceConsole? console, string? resource, PixelRect? window)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Slot = slot ?? throw new ArgumentNullException(nameof(slot));
        Console = console;
        Resource = resource;
        Window = window;
    }

    /// <summary>
    /// Console it belongs to, or null when it suits every console.
    /// </summary>
    public SourceConsole? Console { get; }
    /// <summary>
    /// True when there is no art, only the screenshot and captions.
    /// </summary>
    public bool IsPlain => Resource is null;
    /// <summary>
    /// Stable identifier.
    /// </summary>
    public string Key { get; }
    /// <summary>
    /// Name shown to the user.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// Image file, or null for no art at all.
    /// </summary>
    public string? Resource { get; }
    /// <summary>
    /// Slot the art is sized for.
    /// </summary>
    public ImageSlot Slot { get; }
    /// <summary>
    /// Where the screenshot goes, or null when the slot shows none.
    /// </summary>
    public PixelRect? Window { get; }

    /// <inheritdoc/>
    public override string ToString() => Name;
}
