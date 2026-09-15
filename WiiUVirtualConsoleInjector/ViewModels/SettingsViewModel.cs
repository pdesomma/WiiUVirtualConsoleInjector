using PD.WiiU.VirtualConsole;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// The settings page: folders and which warnings still show.
/// </summary>
public sealed class SettingsViewModel : PageViewModel
{
    /// <summary>
    /// Creates a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    /// <param name="settings">Live settings.</param>
    /// <param name="dialogs">Folder picker.</param>
    /// <param name="paths">Per-user folders.</param>
    /// <param name="links">Opens a folder in the file manager.</param>
    /// <param name="sdCard">Lists and detects removable drives.</param>
    public SettingsViewModel(ISettingsService settings, IDialogService dialogs, AppPaths paths, ILinkOpener links, ISdCard sdCard)
        : base("Settings", "settings.png")
    {
        if (settings is null)
            throw new ArgumentNullException(nameof(settings));
        if (dialogs is null)
            throw new ArgumentNullException(nameof(dialogs));
        if (paths is null)
            throw new ArgumentNullException(nameof(paths));
        if (links is null)
            throw new ArgumentNullException(nameof(links));
        if (sdCard is null)
            throw new ArgumentNullException(nameof(sdCard));

        BaseFolder = new FolderSettingViewModel("Base store folder", () => settings.BasePath, (s, v) => s with { BasePath = v }, settings, dialogs, links);
        OutputFolder = new FolderSettingViewModel("Output folder", () => settings.OutputPath, (s, v) => s with { OutputPath = v }, settings, dialogs, links);
        WorkFolder = new FolderSettingViewModel("Work folder", () => settings.WorkPath, (s, v) => s with { WorkPath = v }, settings, dialogs, links);
        Folders = new[] { BaseFolder, OutputFolder, WorkFolder };
        SdCard = new SdCardSettingViewModel(sdCard, settings, links);
        Warnings = Enum.GetValues<InjectionWarning>().Select(w => new WarningSettingViewModel(w, settings)).ToArray();
        DataFolder = paths.DataFolder;
    }

    /// <summary>
    /// Where bases are kept.
    /// </summary>
    public FolderSettingViewModel BaseFolder { get; }
    /// <summary>
    /// The SD card and whether finished titles are copied to it.
    /// </summary>
    public SdCardSettingViewModel SdCard { get; }
    /// <summary>
    /// Root for settings, keys and caches.
    /// </summary>
    public string DataFolder { get; }
    /// <summary>
    /// Every folder setting, in display order.
    /// </summary>
    public IReadOnlyList<FolderSettingViewModel> Folders { get; }
    /// <summary>
    /// Where injected titles go.
    /// </summary>
    public FolderSettingViewModel OutputFolder { get; }
    /// <summary>
    /// Every warning, in display order.
    /// </summary>
    public IReadOnlyList<WarningSettingViewModel> Warnings { get; }
    /// <summary>
    /// Scratch space for injections.
    /// </summary>
    public FolderSettingViewModel WorkFolder { get; }

    /// <inheritdoc/>
    public override Task ActivateAsync()
    {
        foreach (var folder in Folders)
            folder.Refresh();
        foreach (var warning in Warnings)
            warning.Refresh();
        return Task.CompletedTask;
    }
}
