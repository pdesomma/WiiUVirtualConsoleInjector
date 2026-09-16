using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Infrastructure;
using PD.WiiU.VirtualConsole.Ports;

namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// An <see cref="IBaseStore"/> that follows the base path in settings as it changes.
/// </summary>
public sealed class CurrentBaseStore : IBaseStore
{
    private readonly ISettingsService _settings;

    /// <summary>
    /// Creates a new instance of the <see cref="CurrentBaseStore"/> class.
    /// </summary>
    /// <param name="settings">Where the base path comes from.</param>
    public CurrentBaseStore(ISettingsService settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <inheritdoc/>
    public Task<TitleDirectory> ImportAsync(BaseTitle @base, string sourceDirectory, WiiUSharp.Nus.CommonKey? commonKey, IProgress<string>? progress = null, CancellationToken cancellationToken = default) =>
        Store().ImportAsync(@base, sourceDirectory, commonKey, progress, cancellationToken);

    /// <inheritdoc/>
    public TitleDirectory Locate(BaseTitle @base) => Store().Locate(@base);

    /// <inheritdoc/>
    public Task<TitleDirectory> StageAsync(BaseTitle @base, string destination, CancellationToken cancellationToken = default) =>
        Store().StageAsync(@base, destination, cancellationToken);

    private DirectoryBaseStore Store() => new(_settings.BasePath);
}
