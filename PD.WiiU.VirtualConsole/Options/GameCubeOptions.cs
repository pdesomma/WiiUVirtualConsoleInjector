namespace PD.WiiU.VirtualConsole.Options;

/// <summary>
/// GameCube settings.
/// </summary>
public sealed class GameCubeOptions : IConsoleOptions
{
    /// <inheritdoc/>
    public SourceConsole Console => SourceConsole.GameCube;
    /// <summary>
    /// Boot Nintendont with 4:3 forced instead of its default video mode.
    /// </summary>
    public bool ForceFourByThree { get; init; }
    /// <summary>
    /// Nintendont autoboot forwarder to use as main.dol, or null for the embedded build.
    /// </summary>
    public string? ForwarderPath { get; init; }
    /// <summary>
    /// Second disc for two-disc games, or null.
    /// </summary>
    public string? SecondDiscPath { get; init; }
}
