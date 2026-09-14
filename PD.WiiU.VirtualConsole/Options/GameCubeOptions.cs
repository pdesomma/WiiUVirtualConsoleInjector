namespace PD.WiiU.VirtualConsole.Options;

/// <summary>
/// GameCube settings.
/// </summary>
public sealed class GameCubeOptions : IConsoleOptions
{
    /// <inheritdoc/>
    public SourceConsole Console => SourceConsole.GameCube;
    /// <summary>
    /// Second disc for two-disc games, or null.
    /// </summary>
    public string? SecondDiscPath { get; init; }
    /// <summary>
    /// Strip unused data from the disc.
    /// </summary>
    public bool TrimDisc { get; init; } = true;
}
