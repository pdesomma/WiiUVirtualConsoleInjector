namespace PD.WiiU.VirtualConsole.Options;

/// <summary>
/// Nintendo 64 settings.
/// </summary>
public sealed class N64Options : IConsoleOptions
{
    /// <inheritdoc/>
    public SourceConsole Console => SourceConsole.N64;
    /// <summary>
    /// Keep the emulator's darkening filter.
    /// </summary>
    public bool DarkFilter { get; init; }
    /// <summary>
    /// Emulator INI to use instead of the base title's.
    /// </summary>
    public string? IniPath { get; init; }
    /// <summary>
    /// Render in widescreen.
    /// </summary>
    public bool WideScreen { get; init; }
}
