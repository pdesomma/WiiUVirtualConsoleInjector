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
    /// Whether this is a Virtual Console base or a RetroArch core.
    /// </summary>
    TitleKind Kind { get; }

    /// <summary>
    /// Display name.
    /// </summary>
    string Name { get; }
}
