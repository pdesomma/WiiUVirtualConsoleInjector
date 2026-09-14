namespace PD.WiiU.VirtualConsole;

/// <summary>
/// A progress report: the current step and what it is doing.
/// </summary>
/// <param name="Step">Step in progress.</param>
/// <param name="Message">Human-readable detail.</param>
public sealed record InjectionProgress(InjectionStep Step, string Message);
