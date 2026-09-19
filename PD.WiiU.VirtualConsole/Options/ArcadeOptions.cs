namespace PD.WiiU.VirtualConsole.Options;

/// <summary>
/// Arcade and Neo Geo settings: the zips a romset needs beside the game.
/// </summary>
public sealed class ArcadeOptions : IConsoleOptions
{
    /// <summary>
    /// Files copied verbatim into the title's content folder beside the ROM: parent sets, BIOS sets such as neogeo.zip, sample packs.
    /// </summary>
    public IReadOnlyList<string> CompanionPaths { get; init; } = Array.Empty<string>();
    /// <inheritdoc/>
    public SourceConsole Console { get; init; } = SourceConsole.Arcade;
}
