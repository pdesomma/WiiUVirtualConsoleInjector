using CommunityToolkit.Mvvm.Input;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// The caption font setting: which file the generated boot screens are lettered in.
/// </summary>
public sealed partial class CaptionFontSettingViewModel : ViewModelBase
{
    /// <summary>
    /// Shown when no font file is in use.
    /// </summary>
    public const string BundledText = "Nunito (bundled)";

    private static readonly FileFilter[] Filters = { new("Fonts", "*.otf", "*.ttf"), new("All files", "*") };

    private readonly IDialogService _dialogs;
    private readonly ISettingsService _settings;

    /// <summary>
    /// Creates a new instance of the <see cref="CaptionFontSettingViewModel"/> class.
    /// </summary>
    /// <param name="settings">Where the choice is saved.</param>
    /// <param name="dialogs">File picker.</param>
    public CaptionFontSettingViewModel(ISettingsService settings, IDialogService dialogs)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
    }

    /// <summary>
    /// True while a font file was chosen by hand rather than found.
    /// </summary>
    public bool IsChosen => _settings.Current.CaptionFontPath is not null;
    /// <summary>
    /// The font file in force, or the bundled fallback.
    /// </summary>
    public string Status => _settings.CaptionFontPath ?? BundledText;

    /// <summary>
    /// Lets the user pick a font file; nothing changes when cancelled.
    /// </summary>
    [RelayCommand]
    private async Task BrowseAsync()
    {
        var picked = await _dialogs.PickOpenFileAsync("Caption font", Filters);
        if (picked is not null)
            Set(picked);
    }

    /// <summary>
    /// Forgets the chosen file and goes back to whatever can be found.
    /// </summary>
    [RelayCommand]
    private void Reset() => Set(null);

    /// <summary>
    /// Saves a path and refreshes.
    /// </summary>
    /// <param name="path">Chosen file, or null for none.</param>
    private void Set(string? path)
    {
        _settings.Update(s => s with { CaptionFontPath = path });
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(IsChosen));
    }
}
