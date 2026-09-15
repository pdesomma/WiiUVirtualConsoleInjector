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
    public SettingsViewModel(ISettingsService settings, IDialogService dialogs, AppPaths paths)
        : base("Settings")
    {
        if (settings is null)
            throw new ArgumentNullException(nameof(settings));
        if (dialogs is null)
            throw new ArgumentNullException(nameof(dialogs));
        if (paths is null)
            throw new ArgumentNullException(nameof(paths));

        BaseFolder = new FolderSettingViewModel("Base store folder", () => settings.BasePath, (s, v) => s with { BasePath = v }, settings, dialogs);
        OutputFolder = new FolderSettingViewModel("Output folder", () => settings.OutputPath, (s, v) => s with { OutputPath = v }, settings, dialogs);
        WorkFolder = new FolderSettingViewModel("Work folder", () => settings.WorkPath, (s, v) => s with { WorkPath = v }, settings, dialogs);
        Folders = new[] { BaseFolder, OutputFolder, WorkFolder };
        Warnings = Enum.GetValues<InjectionWarning>().Select(w => new WarningSettingViewModel(w, settings)).ToArray();
        DataFolder = paths.DataFolder;
    }

    /// <summary>
    /// Where bases are kept.
    /// </summary>
    public FolderSettingViewModel BaseFolder { get; }
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
