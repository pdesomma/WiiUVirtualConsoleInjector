namespace PD.WiiU.VirtualConsole.Options;

/// <summary>
/// Settings specific to one <see cref="SourceConsole"/>.
/// </summary>
public interface IConsoleOptions
{
    /// <summary>
    /// Console the settings apply to.
    /// </summary>
    SourceConsole Console { get; }
}
