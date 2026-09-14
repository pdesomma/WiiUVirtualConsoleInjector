namespace PD.WiiU.VirtualConsole.Ports;

/// <summary>
/// Puts a ROM into a staged base for one console.
/// </summary>
public interface IRomInjector
{
    /// <summary>
    /// Console this injector handles.
    /// </summary>
    SourceConsole Console { get; }

    /// <summary>
    /// Replaces the base game with the ROM and applies console-specific options.
    /// </summary>
    /// <param name="injection">ROM and options.</param>
    /// <param name="title">Staged base to modify in place.</param>
    /// <param name="progress">Receives step detail.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    Task InjectAsync(Injection injection, TitleDirectory title, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
}
