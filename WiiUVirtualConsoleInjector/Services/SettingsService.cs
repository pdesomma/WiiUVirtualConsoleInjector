using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Ports;

namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// Default <see cref="ISettingsService"/> over an <see cref="ISettingsStore"/>.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    /// <summary>
    /// What every save toasts.
    /// </summary>
    public const string SavedText = "Saved";

    private readonly AppPaths _paths;
    private readonly ISdCard _sdCard;
    private readonly ISettingsStore _store;
    private readonly IToastService _toasts;

    /// <summary>
    /// Creates a new instance of the <see cref="SettingsService"/> class and loads what is saved.
    /// </summary>
    /// <param name="store">Where settings persist.</param>
    /// <param name="paths">Defaults for unset paths.</param>
    /// <param name="toasts">Told after each save.</param>
    /// <param name="sdCard">Detects the card when no path is set.</param>
    public SettingsService(ISettingsStore store, AppPaths paths, IToastService toasts, ISdCard sdCard)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _toasts = toasts ?? throw new ArgumentNullException(nameof(toasts));
        _sdCard = sdCard ?? throw new ArgumentNullException(nameof(sdCard));
        Current = _store.Load();
    }

    /// <inheritdoc/>
    public event EventHandler? Changed;

    /// <inheritdoc/>
    public string BasePath => Current.BasePath ?? _paths.DefaultBasePath;

    /// <inheritdoc/>
    public AppSettings Current { get; private set; }

    /// <inheritdoc/>
    public string OutputPath => Current.OutputPath ?? _paths.DefaultOutputPath;

    /// <inheritdoc/>
    public string SdPath => Current.SdPath ?? _sdCard.Detect()?.RootPath ?? string.Empty;
    /// <inheritdoc/>
    public string WorkPath => Current.WorkPath ?? _paths.WorkFolder;

    /// <inheritdoc/>
    public void Update(Func<AppSettings, AppSettings> change)
    {
        if (change is null)
            throw new ArgumentNullException(nameof(change));

        Current = change(Current) ?? throw new InvalidOperationException("Settings change produced null.");
        _store.Save(Current);
        Changed?.Invoke(this, EventArgs.Empty);
        _toasts.Show(ToastKind.Success, SavedText, "Settings updated.");
    }
}
