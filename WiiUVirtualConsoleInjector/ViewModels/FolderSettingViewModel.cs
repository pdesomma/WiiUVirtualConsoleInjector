using CommunityToolkit.Mvvm.Input;
using PD.WiiU.VirtualConsole;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// One folder setting: its effective path, a picker and a reset.
/// </summary>
public sealed partial class FolderSettingViewModel : ViewModelBase
{
    private readonly Func<AppSettings, string?, AppSettings> _apply;
    private readonly IDialogService _dialogs;
    private readonly Func<string> _effective;
    private readonly ILinkOpener _links;
    private readonly ISettingsService _settings;

    /// <summary>
    /// Creates a new instance of the <see cref="FolderSettingViewModel"/> class.
    /// </summary>
    /// <param name="label">Name shown to the user.</param>
    /// <param name="effective">The path in force, default included.</param>
    /// <param name="apply">Writes a chosen path, or null for the default, into settings.</param>
    /// <param name="settings">Where the change is saved.</param>
    /// <param name="dialogs">Folder picker.</param>
    /// <param name="links">Opens the folder in the file manager.</param>
    public FolderSettingViewModel(string label, Func<string> effective, Func<AppSettings, string?, AppSettings> apply, ISettingsService settings, IDialogService dialogs, ILinkOpener links)
    {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        _effective = effective ?? throw new ArgumentNullException(nameof(effective));
        _apply = apply ?? throw new ArgumentNullException(nameof(apply));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _links = links ?? throw new ArgumentNullException(nameof(links));
    }

    /// <summary>
    /// The path in force.
    /// </summary>
    public string Folder => _effective();
    /// <summary>
    /// Name shown to the user.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Re-reads the path after settings changed elsewhere.
    /// </summary>
    public void Refresh() => OnPropertyChanged(nameof(Folder));

    /// <summary>
    /// Lets the user pick a folder; nothing changes when cancelled.
    /// </summary>
    [RelayCommand]
    private async Task BrowseAsync()
    {
        var picked = await _dialogs.PickFolderAsync(Label, Directory.Exists(Folder) ? Folder : null);
        if (picked is not null)
            Set(picked);
    }

    /// <summary>
    /// Shows the folder in the file manager.
    /// </summary>
    [RelayCommand]
    private Task OpenAsync() => _links.OpenFolderAsync(Folder);

    /// <summary>
    /// Returns the setting to its default.
    /// </summary>
    [RelayCommand]
    private void Reset() => Set(null);

    /// <summary>
    /// Saves a path and refreshes.
    /// </summary>
    /// <param name="folder">Chosen path, or null for the default.</param>
    private void Set(string? folder)
    {
        _settings.Update(s => _apply(s, folder));
        Refresh();
    }
}
