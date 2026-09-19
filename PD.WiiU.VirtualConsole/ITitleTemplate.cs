namespace PD.WiiU.VirtualConsole;

/// <summary>
/// What an injection is built on: a stock Virtual Console title or a bundled RetroArch core.
/// </summary>
public interface ITitleTemplate
{
    /// <summary>
    /// Console it emulates.
    /// </summary>
    SourceConsole Console { get; }

    /// <summary>
    /// Display name.
    /// </summary>
    string Name { get; }
}
