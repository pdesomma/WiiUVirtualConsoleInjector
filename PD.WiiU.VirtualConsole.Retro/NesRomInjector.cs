using PD.WiiU.VirtualConsole.Options;
using PD.WiiU.VirtualConsole.Ports;

namespace PD.WiiU.VirtualConsole.Retro;

/// <summary>
/// Injects an iNES ROM into a NES Virtual Console base.
/// </summary>
public sealed class NesRomInjector : IRomInjector
{
    /// <inheritdoc/>
    public SourceConsole Console => SourceConsole.Nes;

    /// <inheritdoc/>
    public IReadOnlyList<BaseIssue> Inspect(TitleDirectory title)
    {
        if (title is null)
            throw new ArgumentNullException(nameof(title));

        return RetroExecutable.Inspect(title, nes: true);
    }

    /// <inheritdoc/>
    public Task InjectAsync(Injection injection, TitleDirectory title, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (injection is null)
            throw new ArgumentNullException(nameof(injection));
        if (title is null)
            throw new ArgumentNullException(nameof(title));
        if (injection.Console != SourceConsole.Nes)
            throw new ArgumentException($"Injection is for {injection.Console}.", nameof(injection));

        var options = injection.Options as NesOptions ?? new NesOptions();
        RetroExecutable.Inject(injection, title, nes: true, options.PixelPerfect, progress, cancellationToken);
        return Task.CompletedTask;
    }
}
