using PD.WiiU.VirtualConsole.Options;
using PD.WiiU.VirtualConsole.Ports;

namespace PD.WiiU.VirtualConsole.Retro;

/// <summary>
/// Injects a headerless SNES ROM into a SNES Virtual Console base.
/// </summary>
public sealed class SnesRomInjector : IRomInjector
{
    /// <inheritdoc/>
    public SourceConsole Console => SourceConsole.Snes;

    /// <inheritdoc/>
    public IReadOnlyList<BaseIssue> Inspect(TitleDirectory title)
    {
        if (title is null)
            throw new ArgumentNullException(nameof(title));

        return RetroExecutable.Inspect(title, nes: false);
    }

    /// <inheritdoc/>
    public Task InjectAsync(Injection injection, TitleDirectory title, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (injection is null)
            throw new ArgumentNullException(nameof(injection));
        if (title is null)
            throw new ArgumentNullException(nameof(title));
        if (injection.Console != SourceConsole.Snes)
            throw new ArgumentException($"Injection is for {injection.Console}.", nameof(injection));

        var options = injection.Options as SnesOptions ?? new SnesOptions();
        RetroExecutable.Inject(injection, title, nes: false, options.PixelPerfect, progress, cancellationToken);
        return Task.CompletedTask;
    }
}
