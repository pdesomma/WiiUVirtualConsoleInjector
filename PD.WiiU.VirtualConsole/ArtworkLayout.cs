namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Where a screenshot sits inside a frame; the shapes the console templates were drawn around.
/// </summary>
public sealed class ArtworkLayout
{
    /// <summary>
    /// The Game Boy Advance's wide screen.
    /// </summary>
    public static readonly ArtworkLayout Gba = new("Gba", new PixelRect(3, 17, 122, 81), new PixelRect(132, 260, 399, 266));
    /// <summary>
    /// The Game Boy and Game Boy Color's near-square screen.
    /// </summary>
    public static readonly ArtworkLayout Gbc = new("Gbc", new PixelRect(3, 9, 122, 92), new PixelRect(183, 260, 296, 266));
    /// <summary>
    /// The 4:3 screen every other console uses.
    /// </summary>
    public static readonly ArtworkLayout Standard = new("Standard", new PixelRect(3, 9, 122, 92), new PixelRect(131, 249, 400, 300));
    /// <summary>
    /// The Wii's wide frame.
    /// </summary>
    public static readonly ArtworkLayout Wii = new("Wii", new PixelRect(0, 23, 128, 94), new PixelRect(224, 200, 832, 333));

    private ArtworkLayout(string name, PixelRect icon, PixelRect boot)
    {
        Name = name;
        IconArea = icon;
        BootArea = boot;
    }

    /// <summary>
    /// Where the screenshot goes on a 1280x720 boot screen.
    /// </summary>
    public PixelRect BootArea { get; }
    /// <summary>
    /// Where the screenshot goes on a 128x128 icon.
    /// </summary>
    public PixelRect IconArea { get; }
    /// <summary>
    /// Name of the layout.
    /// </summary>
    public string Name { get; }

    /// <inheritdoc/>
    public override string ToString() => Name;
}
