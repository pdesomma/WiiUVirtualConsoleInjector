namespace PD.WiiU.VirtualConsole;

/// <summary>
/// One frame a screenshot can be dressed in: the boot screen art, the matching icon art, and where the screenshot goes.
/// </summary>
public sealed class ArtworkTemplate
{
    /// <summary>
    /// Creates a new instance of the <see cref="ArtworkTemplate"/> class.
    /// </summary>
    /// <param name="key">Stable identifier.</param>
    /// <param name="name">Name shown to the user.</param>
    /// <param name="console">Console it belongs to.</param>
    /// <param name="layout">Where the screenshot sits.</param>
    /// <param name="bootFrame">1280x720 frame file, or null to leave the boot screen bare.</param>
    /// <param name="iconFrame">128x128 frame file, or null for the plain Virtual Console icon.</param>
    public ArtworkTemplate(string key, string name, SourceConsole console, ArtworkLayout layout, string? bootFrame, string? iconFrame)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Layout = layout ?? throw new ArgumentNullException(nameof(layout));
        Console = console;
        BootFrame = bootFrame;
        IconFrame = iconFrame;
    }

    /// <summary>
    /// 1280x720 frame file, or null.
    /// </summary>
    public string? BootFrame { get; }
    /// <summary>
    /// Console it belongs to.
    /// </summary>
    public SourceConsole Console { get; }
    /// <summary>
    /// 128x128 frame file, or null.
    /// </summary>
    public string? IconFrame { get; }
    /// <summary>
    /// Stable identifier.
    /// </summary>
    public string Key { get; }
    /// <summary>
    /// Where the screenshot sits.
    /// </summary>
    public ArtworkLayout Layout { get; }
    /// <summary>
    /// Name shown to the user.
    /// </summary>
    public string Name { get; }

    /// <inheritdoc/>
    public override string ToString() => Name;
}
