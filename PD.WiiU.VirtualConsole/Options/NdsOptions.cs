namespace PD.WiiU.VirtualConsole.Options;

/// <summary>
/// Nintendo DS settings.
/// </summary>
public sealed class NdsOptions : IConsoleOptions
{
    /// <summary>
    /// Brightness the base ships with; anything else is written to configuration_cafe.json.
    /// </summary>
    public const int DefaultBrightness = 80;

    /// <summary>
    /// Display brightness percent; 80 is the stock darkened look.
    /// </summary>
    public int Brightness { get; init; } = DefaultBrightness;
    /// <inheritdoc/>
    public SourceConsole Console => SourceConsole.Nds;
    /// <summary>
    /// Bundled extra layouts to copy over the title; a <see cref="LayoutScreensPath"/> wins over it.
    /// </summary>
    public NdsLayoutPack LayoutPack { get; init; }
    /// <summary>
    /// Folder of replacement layout screens to copy over the title, or null to keep the base's.
    /// </summary>
    public string? LayoutScreensPath { get; init; }
    /// <summary>
    /// Pixel-art upscaler level; 0 is off, values above 32 are unverified.
    /// </summary>
    public int PixelArtUpscaler { get; init; }
}
