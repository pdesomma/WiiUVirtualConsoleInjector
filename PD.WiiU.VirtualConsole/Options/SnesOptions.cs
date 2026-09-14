namespace PD.WiiU.VirtualConsole.Options;

/// <summary>
/// SNES settings.
/// </summary>
public sealed class SnesOptions : IConsoleOptions
{
    /// <inheritdoc/>
    public SourceConsole Console => SourceConsole.Snes;
    /// <summary>
    /// Integer-scale the picture.
    /// </summary>
    public bool PixelPerfect { get; init; }
}
