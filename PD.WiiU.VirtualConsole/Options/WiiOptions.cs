namespace PD.WiiU.VirtualConsole.Options;

/// <summary>
/// Wii settings.
/// </summary>
public sealed class WiiOptions : IConsoleOptions
{
    /// <summary>
    /// GCT cheat file to apply, or null.
    /// </summary>
    public string? CheatCodesPath { get; init; }
    /// <inheritdoc/>
    public SourceConsole Console => SourceConsole.Wii;
    /// <summary>
    /// GamePad controller emulation.
    /// </summary>
    public WiiControllerMode ControllerMode { get; init; } = WiiControllerMode.ClassicController;
    /// <summary>
    /// Force PAL video mode.
    /// </summary>
    public bool ForcePal { get; init; }
    /// <summary>
    /// Halve the vertical filter strength.
    /// </summary>
    public bool HalfVerticalFilter { get; init; }
    /// <summary>
    /// Swap L and R with ZL and ZR.
    /// </summary>
    public bool LrPatch { get; init; }
    /// <summary>
    /// Pass Wii Remote input through to the game.
    /// </summary>
    public bool Passthrough { get; init; } = true;
    /// <summary>
    /// Remove the deflicker filter.
    /// </summary>
    public bool RemoveDeflicker { get; init; }
    /// <summary>
    /// Remove dithering.
    /// </summary>
    public bool RemoveDithering { get; init; }
    /// <summary>
    /// Region to patch the disc to, or null to leave it.
    /// </summary>
    public Region? TargetRegion { get; init; }
    /// <summary>
    /// Strip unused partitions from the disc.
    /// </summary>
    public bool TrimDisc { get; init; } = true;
}
