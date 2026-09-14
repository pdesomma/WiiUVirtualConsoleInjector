namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Replacement images for the title; slots left null keep the base title's image.
/// </summary>
public sealed class Artwork
{
    /// <summary>
    /// No replacements.
    /// </summary>
    public static readonly Artwork None = new();

    /// <summary>
    /// GamePad boot screen path.
    /// </summary>
    public string? BootDrc { get; init; }
    /// <summary>
    /// Boot logo path.
    /// </summary>
    public string? BootLogo { get; init; }
    /// <summary>
    /// TV boot screen path.
    /// </summary>
    public string? BootTv { get; init; }
    /// <summary>
    /// Menu icon path.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Path supplied for the slot, or null.
    /// </summary>
    public string? PathFor(ImageSlot slot)
    {
        if (slot == ImageSlot.BootDrc) return BootDrc;
        if (slot == ImageSlot.BootLogo) return BootLogo;
        if (slot == ImageSlot.BootTv) return BootTv;
        if (slot == ImageSlot.Icon) return Icon;
        throw new ArgumentOutOfRangeException(nameof(slot), slot, "Unknown image slot.");
    }
}
