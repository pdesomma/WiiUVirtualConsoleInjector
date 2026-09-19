namespace PD.WiiU.VirtualConsole.Ports;

/// <summary>
/// Puts a ROM into a staged template for one console, built on a base or on a core.
/// </summary>
public interface IRomInjector
{
    /// <summary>
    /// Console this injector handles.
    /// </summary>
    SourceConsole Console { get; }

    /// <summary>
    /// Which template kind it fills.
    /// </summary>
    TitleKind Kind { get; }

    /// <summary>
    /// Lists what the base lacks for this console; empty when it can be injected.
    /// </summary>
    /// <param name="title">Base to inspect, staged or in place.</param>
    IReadOnlyList<BaseIssue> Inspect(TitleDirectory title);

    /// <summary>
    /// Replaces the base game with the ROM and applies console-specific options.
    /// </summary>
    /// <param name="injection">ROM and options.</param>
    /// <param name="title">Staged base to modify in place.</param>
    /// <param name="progress">Receives step detail.</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    Task InjectAsync(Injection injection, TitleDirectory title, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
}
