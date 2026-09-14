namespace PD.WiiU.VirtualConsole.Options;

/// <summary>
/// NES settings.
/// </summary>
public sealed class NesOptions : IConsoleOptions
{
    /// <inheritdoc/>
    public SourceConsole Console => SourceConsole.Nes;
    /// <summary>
    /// Integer-scale the picture.
    /// </summary>
    public bool PixelPerfect { get; init; }
}
