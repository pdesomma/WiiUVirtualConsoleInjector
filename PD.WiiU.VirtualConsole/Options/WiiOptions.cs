using WiiUSharp;

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
    /// For a channel WAD: boot with 4:3 forced.
    /// </summary>
    public bool ForceFourByThree { get; init; }
    /// <summary>
    /// For a channel WAD: booter to use as main.dol, or null for the embedded wiivc_chan_booter.
    /// </summary>
    public string? ForwarderPath { get; init; }
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
    /// Rebuild the disc with only the game partition; also required by every main.dol patch.
    /// </summary>
    public bool TrimDisc { get; init; } = true;
    /// <summary>
    /// Video standard to force in main.dol.
    /// </summary>
    public WiiVideoMode VideoMode { get; init; }
}
