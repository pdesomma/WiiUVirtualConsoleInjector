namespace PD.WiiU.VirtualConsole.Options;

/// <summary>
/// Nintendo 64 settings.
/// </summary>
public sealed class N64Options : IConsoleOptions
{
    /// <inheritdoc/>
    public SourceConsole Console => SourceConsole.N64;
    /// <summary>
    /// Emulator INI to use; null writes an empty one.
    /// </summary>
    public string? IniPath { get; init; }
    /// <summary>
    /// Switch off the darkening overlay the base draws over the game.
    /// </summary>
    public bool RemoveDarkFilter { get; init; }
    /// <summary>
    /// Render in widescreen.
    /// </summary>
    public bool WideScreen { get; init; }
}
