using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PD.WiiU.VirtualConsole;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// The SD card setting: which removable drive to use and whether finished titles are copied to it.
/// </summary>
public sealed partial class SdCardSettingViewModel : ViewModelBase
{
    /// <summary>
    /// Shown in place of a drive when none was found.
    /// </summary>
    public const string NoDriveText = "No removable drive found";

    private readonly ILinkOpener _links;
    private readonly ISdCard _sdCard;
    private readonly ISettingsService _settings;

    private bool _syncing;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCard))]
    private RemovableDrive? _selectedDrive;

    /// <summary>
    /// Creates a new instance of the <see cref="SdCardSettingViewModel"/> class.
    /// </summary>
    /// <param name="sdCard">Lists and detects drives.</param>
    /// <param name="settings">Where the choice is saved.</param>
    /// <param name="links">Opens the card in the file manager.</param>
    public SdCardSettingViewModel(ISdCard sdCard, ISettingsService settings, ILinkOpener links)
    {
        _sdCard = sdCard ?? throw new ArgumentNullException(nameof(sdCard));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _links = links ?? throw new ArgumentNullException(nameof(links));
        Rescan();
    }

    /// <summary>
    /// True while finished titles are copied to the card.
    /// </summary>
    public bool CopyAfterInject
    {
        get => _settings.Current.CopyToSdCard;
        set
        {
            if (value == CopyAfterInject)
                return;

            _settings.Update(s => s with { CopyToSdCard = value });
            OnPropertyChanged();
        }
    }
    /// <summary>
    /// Removable drives on offer.
    /// </summary>
    public ObservableCollection<RemovableDrive> Drives { get; } = new();
    /// <summary>
    /// True when a drive is chosen.
    /// </summary>
    public bool HasCard => SelectedDrive is not null;
    /// <summary>
    /// Root of the card in force, or a note that there is none.
    /// </summary>
    public string Status => SelectedDrive is { } drive ? Path.Combine(drive.RootPath, SdCard.InstallFolder) : NoDriveText;

    /// <summary>
    /// Re-reads the drives and the saved choice.
    /// </summary>
    [RelayCommand]
    public void Rescan()
    {
        _syncing = true;
        try
        {
            Drives.Clear();
            foreach (var drive in _sdCard.Drives())
                Drives.Add(drive);

            var saved = _settings.SdPath;
            SelectedDrive = Drives.FirstOrDefault(d => string.Equals(d.RootPath, saved, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _syncing = false;
        }
        OnPropertyChanged(nameof(Status));
    }

    /// <summary>
    /// Shows the card's install folder in the file manager.
    /// </summary>
    [RelayCommand]
    private Task OpenAsync() => SelectedDrive is { } drive ? _links.OpenFolderAsync(Path.Combine(drive.RootPath, SdCard.InstallFolder)) : Task.FromResult(false);

    /// <summary>
    /// Saves the drive the user picked; clearing it goes back to detection.
    /// </summary>
    /// <param name="value">Drive now selected.</param>
    partial void OnSelectedDriveChanged(RemovableDrive? value)
    {
        OnPropertyChanged(nameof(Status));
        if (_syncing)
            return;

        _settings.Update(s => s with { SdPath = value?.RootPath });
    }
}
