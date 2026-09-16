using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PD.WiiU.VirtualConsole.Ports;
using PD.WiiU.VirtualConsole.Wii;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// Nintendont on the SD card: whether the loader and its nincfg.bin are there, a button to put the loader there, and the settings the config file carries.
/// </summary>
public sealed partial class NintendontSettingViewModel : ViewModelBase
{
    private readonly Func<string?> _cardRoot;
    private readonly IDialogService _dialogs;
    private readonly INintendontSource _source;

    [ObservableProperty]
    private bool _arcadeMode;
    [ObservableProperty]
    private bool _broadbandEmulation;
    [ObservableProperty]
    private bool _cheats;
    [ObservableProperty]
    private bool _classicControllerRumble;
    [ObservableProperty]
    private NintendontForcedMode _forcedMode = NintendontForcedMode.Ntsc;
    [ObservableProperty]
    private bool _forceProgressive;
    [ObservableProperty]
    private bool _forceWidescreen;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand), nameof(WriteConfigCommand))]
    private bool _isBusy;
    [ObservableProperty]
    private NintendontLanguage _language = NintendontLanguage.Auto;
    [ObservableProperty]
    private bool _memoryCardEmulation = true;
    [ObservableProperty]
    private bool _memoryCardShared;
    [ObservableProperty]
    private int _memoryCardSize = 2;
    [ObservableProperty]
    private int _pads = NintendontConfig.MaxPads;
    [ObservableProperty]
    private bool _patchPal50;
    [ObservableProperty]
    private string? _progress;
    [ObservableProperty]
    private bool _progressive;
    [ObservableProperty]
    private bool _removeLimit;
    [ObservableProperty]
    private bool _skipIpl;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsForcing))]
    private NintendontVideo _video = NintendontVideo.Auto;
    [ObservableProperty]
    private bool _wiiUWidescreen;

    /// <summary>
    /// Creates a new instance of the <see cref="NintendontSettingViewModel"/> class.
    /// </summary>
    /// <param name="source">Where Nintendont is downloaded from.</param>
    /// <param name="cardRoot">Root of the picked card, or null.</param>
    /// <param name="dialogs">Error dialogs.</param>
    public NintendontSettingViewModel(INintendontSource source, Func<string?> cardRoot, IDialogService dialogs)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _cardRoot = cardRoot ?? throw new ArgumentNullException(nameof(cardRoot));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
    }

    /// <summary>
    /// Text for what the card holds.
    /// </summary>
    public string ConfigStatus => _cardRoot() is null ? "" : HasConfig ? "nincfg.bin is on the card." : "No nincfg.bin on the card; the forwarder needs one.";
    /// <summary>
    /// Modes to force.
    /// </summary>
    public IReadOnlyList<NintendontForcedMode> ForcedModes { get; } = new[] { NintendontForcedMode.Ntsc, NintendontForcedMode.Pal60, NintendontForcedMode.Pal50, NintendontForcedMode.MPal };
    /// <summary>
    /// True when the card holds nincfg.bin.
    /// </summary>
    public bool HasConfig => _cardRoot() is { } root && File.Exists(Path.Combine(root, NintendontConfig.FileName));
    /// <summary>
    /// True when the card holds apps/nintendont/boot.dol.
    /// </summary>
    public bool HasLoader => _cardRoot() is { } root && File.Exists(LoaderPath(root));
    /// <summary>
    /// True when a card is picked.
    /// </summary>
    public bool HasCard => _cardRoot() is not null;
    /// <summary>
    /// True when the video setting forces a mode.
    /// </summary>
    public bool IsForcing => Video is NintendontVideo.Force or NintendontVideo.ForceDeflicker;
    /// <summary>
    /// Languages to choose from.
    /// </summary>
    public IReadOnlyList<NintendontLanguage> Languages { get; } = new[] { NintendontLanguage.Auto, NintendontLanguage.English, NintendontLanguage.German, NintendontLanguage.French, NintendontLanguage.Spanish, NintendontLanguage.Italian, NintendontLanguage.Dutch };
    /// <summary>
    /// Text for the loader.
    /// </summary>
    public string LoaderStatus => _cardRoot() is null ? "Pick the card above first." : HasLoader ? "Nintendont is on the card (apps/nintendont/boot.dol)." : "Nintendont is not on the card; GameCube injects will not boot until it is.";
    /// <summary>
    /// Card sizes as block counts, by index.
    /// </summary>
    public IReadOnlyList<int> MemoryCardSizes { get; } = Enumerable.Range(0, NintendontConfig.MaxMemoryCardSize + 1).ToArray();
    /// <summary>
    /// Video choices.
    /// </summary>
    public IReadOnlyList<NintendontVideo> Videos { get; } = new[] { NintendontVideo.Auto, NintendontVideo.Force, NintendontVideo.ForceDeflicker, NintendontVideo.None };

    /// <summary>
    /// The fields as a configuration.
    /// </summary>
    public NintendontConfig Build() => new()
    {
        ArcadeMode = ArcadeMode,
        BroadbandEmulation = BroadbandEmulation,
        Cheats = Cheats,
        ClassicControllerRumble = ClassicControllerRumble,
        ForcedMode = ForcedMode,
        ForceProgressive = ForceProgressive,
        ForceWidescreen = ForceWidescreen,
        Language = Language,
        MemoryCardEmulation = MemoryCardEmulation,
        MemoryCardShared = MemoryCardShared,
        MemoryCardSize = MemoryCardSize,
        Pads = (uint)Pads,
        PatchPal50 = PatchPal50,
        Progressive = Progressive,
        RemoveLimit = RemoveLimit,
        SkipIpl = SkipIpl,
        Video = Video,
        WiiUWidescreen = WiiUWidescreen,
    };

    /// <summary>
    /// Fills the fields from a configuration.
    /// </summary>
    /// <param name="config">Settings to show.</param>
    public void Load(NintendontConfig config)
    {
        if (config is null)
            throw new ArgumentNullException(nameof(config));

        ArcadeMode = config.ArcadeMode;
        BroadbandEmulation = config.BroadbandEmulation;
        Cheats = config.Cheats;
        ClassicControllerRumble = config.ClassicControllerRumble;
        ForcedMode = config.ForcedMode;
        ForceProgressive = config.ForceProgressive;
        ForceWidescreen = config.ForceWidescreen;
        Language = Languages.Contains(config.Language) ? config.Language : NintendontLanguage.Auto;
        MemoryCardEmulation = config.MemoryCardEmulation;
        MemoryCardShared = config.MemoryCardShared;
        MemoryCardSize = config.MemoryCardSize;
        Pads = (int)config.Pads;
        PatchPal50 = config.PatchPal50;
        Progressive = config.Progressive;
        RemoveLimit = config.RemoveLimit;
        SkipIpl = config.SkipIpl;
        Video = Videos.Contains(config.Video) ? config.Video : NintendontVideo.Auto;
        WiiUWidescreen = config.WiiUWidescreen;
    }

    /// <summary>
    /// Re-reads the card: presence of the files, and the config's settings when there is one.
    /// </summary>
    public void Refresh()
    {
        foreach (var name in new[] { nameof(HasCard), nameof(HasLoader), nameof(HasConfig), nameof(LoaderStatus), nameof(ConfigStatus) })
            OnPropertyChanged(name);
        InstallCommand.NotifyCanExecuteChanged();
        WriteConfigCommand.NotifyCanExecuteChanged();
        if (_cardRoot() is not { } root || !File.Exists(Path.Combine(root, NintendontConfig.FileName)))
            return;
        try
        {
            Load(NintendontConfig.Parse(File.ReadAllBytes(Path.Combine(root, NintendontConfig.FileName))));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException)
        {
        }
    }

    private static string LoaderPath(string root) => Path.Combine(root, "apps", "nintendont", "boot.dol");

    private bool CanAct() => HasCard && !IsBusy;

    /// <summary>
    /// Downloads Nintendont onto the card and writes a nincfg.bin when there is none yet.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task InstallAsync()
    {
        if (_cardRoot() is not { } root)
            return;
        IsBusy = true;
        try
        {
            await _source.InstallAsync(root, new Progress<string>(m => Progress = m));
            if (!File.Exists(Path.Combine(root, NintendontConfig.FileName)))
                File.WriteAllBytes(Path.Combine(root, NintendontConfig.FileName), Build().ToBytes());
            Progress = "Nintendont is on the card.";
        }
        catch (Exception e) when (e is HttpRequestException or IOException or UnauthorizedAccessException)
        {
            Progress = null;
            await _dialogs.ShowErrorAsync("Nintendont", e.Message);
        }
        finally
        {
            IsBusy = false;
            Refresh();
        }
    }

    /// <summary>
    /// Writes the fields to the card's nincfg.bin.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAct))]
    private async Task WriteConfigAsync()
    {
        if (_cardRoot() is not { } root)
            return;
        try
        {
            File.WriteAllBytes(Path.Combine(root, NintendontConfig.FileName), Build().ToBytes());
            Progress = "nincfg.bin written.";
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            await _dialogs.ShowErrorAsync("Nintendont", e.Message);
        }
        Refresh();
    }
}
