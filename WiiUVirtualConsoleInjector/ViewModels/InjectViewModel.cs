using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp;
using WiiUVirtualConsoleInjector.Services;
using WiiUVirtualConsoleInjector.ViewModels.Options;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// The inject page: pick a console, base, ROM, artwork and options, then run the injection.
/// </summary>
public sealed partial class InjectViewModel : PageViewModel, IArrowNavigation
{
    /// <summary>
    /// Title of the confirmation shown before a risky inject.
    /// </summary>
    public const string WarningTitle = "Warning";

    private const string DialogTitle = "Inject";
    private const string GczWarning = "GCZ images take longer to inject than an ISO or GCM, since they are decoded first.\n\nContinue anyway?";
    private const string NdsWarning = "You can only inject NDS ROMs that are not DSi Enhanced (example for not working: Pokémon Black & White).\n\nIf attempting to inject a DSi Enhanced ROM, we will not give you any support with fixing said injection.\n\nContinue?";
    private const string SnesWarning = "You can only inject SNES ROMs that are not using any Co-Processors (example for not working: Star Fox).\n\nIf attempting to inject a ROM in need of a Co-Processor, we will not give you any support with fixing said injection.\n\nContinue?";

    private static readonly FileFilter[] ImageFilters = { new("Images", "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.webp", "*.tga") };
    private static readonly FileFilter[] SoundFilters = { new("Audio", "*.wav", "*.mp3", "*.aiff", "*.aif", "*.btsnd") };

    /// <summary>
    /// Pages of the wizard in order.
    /// </summary>
    public static readonly IReadOnlyList<WizardStep> Steps = new[]
    {
        new WizardStep(1, "Console"),
        new WizardStep(2, "Base"),
        new WizardStep(3, "Game"),
        new WizardStep(4, "Artwork"),
        new WizardStep(5, "Options"),
        new WizardStep(6, "Review & Inject"),
    };

    private readonly IBaseService _bases;
    private readonly IDialogService _dialogs;
    private readonly ICompatibilityLists _compatibility;
    private readonly ICommunityArtwork _communityArtwork;
    private readonly IInjectionHistory _history;
    private readonly IInjectionServiceFactory _injections;
    private readonly INavigationService _navigation;
    private readonly ISdCard _sdCard;
    private readonly ISettingsService _settings;
    private readonly ISoundPlayer _sounds;
    private CancellationTokenSource? _cancellation;
    private TitleIdentity? _identity;

    [ObservableProperty]
    private ConsoleOptionsViewModel _currentOptions;
    [ObservableProperty]
    private string? _currentStep;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReviewOutput))]
    private OutputFormat _format = OutputFormat.Wup;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReviewGame))]
    private bool _gamePad;
    [ObservableProperty]
    private bool _isPlayingSound;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInject))]
    [NotifyCanExecuteChangedFor(nameof(InjectCommand), nameof(CancelCommand), nameof(PreviewSoundCommand))]
    private bool _isRunning;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInject), nameof(HasMissingKeys), nameof(MissingKeysHint))]
    [NotifyCanExecuteChangedFor(nameof(InjectCommand))]
    private IReadOnlyList<string> _missingKeys = Array.Empty<string>();
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInject), nameof(ReviewGame))]
    [NotifyCanExecuteChangedFor(nameof(InjectCommand))]
    private string? _name;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInject), nameof(ReviewGame))]
    [NotifyCanExecuteChangedFor(nameof(InjectCommand))]
    private string? _productId;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInject), nameof(ReviewRom), nameof(HasRom))]
    [NotifyCanExecuteChangedFor(nameof(InjectCommand), nameof(ClearRomCommand))]
    private string? _romPath;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInject), nameof(HasRomFitHint))]
    [NotifyCanExecuteChangedFor(nameof(InjectCommand))]
    private string? _romFitHint;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInject), nameof(BaseHint), nameof(ReviewBase))]
    [NotifyCanExecuteChangedFor(nameof(InjectCommand))]
    private BaseChoice? _selectedBase;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGamePadVisible), nameof(IsTurboCd), nameof(SelectedConsoleName), nameof(RomExtensions), nameof(ReviewGame))]
    private SourceConsole _selectedConsole;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReviewGame))]
    private string? _shortName;
    [ObservableProperty]
    private string? _status;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentWizardStep), nameof(SelectedStep), nameof(CanGoNext), nameof(CanGoPrevious), nameof(IsConsoleStep), nameof(IsBaseStep), nameof(IsGameStep), nameof(IsArtworkStep), nameof(IsOptionsStep), nameof(IsReviewStep))]
    [NotifyCanExecuteChangedFor(nameof(NextStepCommand), nameof(PreviousStepCommand))]
    private int _step = 1;

    /// <summary>
    /// Creates a new instance of the <see cref="InjectViewModel"/> class.
    /// </summary>
    /// <param name="bases">Bases known per console and their status.</param>
    /// <param name="dialogs">Pickers and message boxes.</param>
    /// <param name="injections">Builds the injection service and reports missing keys.</param>
    /// <param name="settings">Work and output folders, suppressed warnings.</param>
    /// <param name="navigation">Lets the page jump to Bases and Keys.</param>
    /// <param name="sdCard">Copies the finished title to the card.</param>
    /// <param name="artwork">Builds icons and boot screens from a screenshot.</param>
    /// <param name="sounds">Plays the boot sound back.</param>
    /// <param name="history">Remembers finished injects.</param>
    /// <param name="compatibility">Community compatibility pages per console.</param>
    /// <param name="communityArtwork">The community artwork repository.</param>
    public InjectViewModel(IBaseService bases, IDialogService dialogs, IInjectionServiceFactory injections, ISettingsService settings, INavigationService navigation, ISdCard sdCard, ArtworkBuilderViewModel artwork, ISoundPlayer sounds, IInjectionHistory history, ICompatibilityLists compatibility, ICommunityArtwork communityArtwork)
        : base("Inject", "inject-icon.png", "M12 3v11 M7.5 10.5L12 15l4.5-4.5 M4 17.5V19a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1v-1.5")
    {
        _bases = bases ?? throw new ArgumentNullException(nameof(bases));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _history = history ?? throw new ArgumentNullException(nameof(history));
        _compatibility = compatibility ?? throw new ArgumentNullException(nameof(compatibility));
        _communityArtwork = communityArtwork ?? throw new ArgumentNullException(nameof(communityArtwork));
        _injections = injections ?? throw new ArgumentNullException(nameof(injections));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _sdCard = sdCard ?? throw new ArgumentNullException(nameof(sdCard));
        ArtworkBuilder = artwork ?? throw new ArgumentNullException(nameof(artwork));
        _sounds = sounds ?? throw new ArgumentNullException(nameof(sounds));
        _sounds.Stopped += (_, _) => IsPlayingSound = false;

        Icon = new PathFieldViewModel(dialogs, "Icon", "128 × 128", ImageFilters) { Glyph = "camera.png" };
        BootTv = new PathFieldViewModel(dialogs, "TV boot screen", "1280 × 720", ImageFilters) { Glyph = "camera.png" };
        BootDrc = new PathFieldViewModel(dialogs, "GamePad boot screen", "854 × 480", ImageFilters) { Glyph = "camera.png" };
        BootLogo = new PathFieldViewModel(dialogs, "Boot logo", "170 × 42", ImageFilters) { Glyph = "camera.png" };
        BootSound = new PathFieldViewModel(dialogs, "Boot sound", "wav, mp3, aiff or a ready btsnd", SoundFilters) { Glyph = "speaker.png" };
        ArtworkBuilder.Built += (_, built) => SlotFor(built.Slot).Path = built.Path;
        CommunityArtwork = new CommunityArtworkViewModel(_communityArtwork, () => SelectedConsole, () => RomPath, () => _settings.WorkPath);
        CommunityArtwork.Applied += (_, files) => TakeCommunityArtwork(files);
        BootSound.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PathFieldViewModel.Path))
            {
                _sounds.Stop();
                PreviewSoundCommand.NotifyCanExecuteChanged();
            }
        };

        _selectedConsole = SourceConsole.Nes;
        _currentOptions = CreateOptions(_selectedConsole);
        ArtworkBuilder.Refresh(_selectedConsole, Name, ShortName);
        Refresh();
    }

    /// <summary>
    /// Why the selected base cannot be used, or null when it can.
    /// </summary>
    public string? BaseHint => SelectedBase switch
    {
        null => "No base is selected for this console.",
        { IsPresent: false } => "This base is not downloaded; get it on Bases & Keys.",
        { KeysOk: false } => "A key this base needs is missing; add it on Bases & Keys.",
        _ => null,
    };

    /// <summary>
    /// True while a later step exists.
    /// </summary>
    /// <inheritdoc/>
    public System.Windows.Input.ICommand NextCommand => NextStepCommand;

    /// <inheritdoc/>
    public string NextHint => "Next step";

    /// <inheritdoc/>
    public System.Windows.Input.ICommand PreviousCommand => PreviousStepCommand;

    /// <inheritdoc/>
    public string PreviousHint => "Previous step";

    public bool CanGoNext => Step < Steps.Count;

    /// <summary>
    /// True while an earlier step exists.
    /// </summary>
    public bool CanGoPrevious => Step > 1;

    /// <summary>
    /// The step on screen.
    /// </summary>
    public WizardStep CurrentWizardStep => Steps[Step - 1];

    /// <summary>
    /// Bases for the selected console.
    /// </summary>
    public ObservableCollection<BaseChoice> Bases { get; } = new();

    /// <summary>
    /// Builds icons and boot screens from a screenshot.
    /// </summary>
    public ArtworkBuilderViewModel ArtworkBuilder { get; }

    /// <summary>
    /// Looks the ROM up in the community artwork repository.
    /// </summary>
    public CommunityArtworkViewModel CommunityArtwork { get; }
    /// <summary>
    /// GamePad boot screen.
    /// </summary>
    public PathFieldViewModel BootDrc { get; }

    /// <summary>
    /// Boot logo.
    /// </summary>
    public PathFieldViewModel BootLogo { get; }

    /// <summary>
    /// Sound played at boot.
    /// </summary>
    public PathFieldViewModel BootSound { get; }

    /// <summary>
    /// TV boot screen.
    /// </summary>
    public PathFieldViewModel BootTv { get; }

    /// <summary>
    /// True when everything an inject needs is in place and none is running.
    /// </summary>
    public bool CanInject =>
        !IsRunning
        && SelectedBase is { IsPresent: true }
        && !string.IsNullOrWhiteSpace(RomPath)
        && !string.IsNullOrWhiteSpace(Name)
        && IsProductIdValid
        && MissingKeys.Count == 0
        && RomFitHint is null;

    /// <summary>
    /// Every console an injection can target.
    /// </summary>
    public IReadOnlyList<SourceConsole> Consoles { get; } = Enum.GetValues<SourceConsole>();

    /// <summary>
    /// True when a key the inject needs is missing.
    /// </summary>
    public bool HasMissingKeys => MissingKeys.Count > 0;

    /// <summary>
    /// True when a boot sound is chosen and nothing is being injected.
    /// </summary>
    public bool CanPreviewSound => BootSound.HasPath && !IsRunning;
    /// <summary>
    /// True once a ROM is picked.
    /// </summary>
    public bool HasRom => !string.IsNullOrWhiteSpace(RomPath);

    /// <summary>
    /// Menu icon.
    /// </summary>
    public PathFieldViewModel Icon { get; }

    /// <summary>
    /// True on the artwork step.
    /// </summary>
    public bool IsArtworkStep => Step == 4;

    /// <summary>
    /// True on the base step.
    /// </summary>
    public bool IsBaseStep => Step == 2;

    /// <summary>
    /// True on the console step.
    /// </summary>
    public bool IsConsoleStep => Step == 1;

    /// <summary>
    /// True for consoles whose titles can advertise GamePad-as-controller use.
    /// </summary>
    public bool IsGamePadVisible => SelectedConsole is SourceConsole.Wii or SourceConsole.GameCube;

    /// <summary>
    /// True on the game step.
    /// </summary>
    public bool IsGameStep => Step == 3;

    /// <summary>
    /// True on the options step.
    /// </summary>
    public bool IsOptionsStep => Step == 5;

    /// <summary>
    /// True on the review step.
    /// </summary>
    public bool IsReviewStep => Step == 6;

    /// <summary>
    /// True when the console also accepts a TurboCD folder as the ROM.
    /// </summary>
    public bool IsTurboCd => SelectedConsole == SourceConsole.Tg16;

    /// <summary>
    /// Progress lines from the running or last inject.
    /// </summary>
    public ObservableCollection<string> Log { get; } = new();

    /// <summary>
    /// Names of the keys still needed, or null when none are.
    /// </summary>
    public string? MissingKeysHint => HasMissingKeys ? $"Missing {string.Join(" and ", MissingKeys)}; add it on Bases & Keys." : null;

    /// <summary>
    /// Base line of the review summary.
    /// </summary>
    public string ReviewBase => SelectedBase is { } b ? $"{b.Base.Name} ({b.Base.Region})" : "Not selected";

    /// <summary>
    /// Output shapes to choose from.
    /// </summary>
    public IReadOnlyList<OutputFormat> Formats { get; } = new[] { OutputFormat.Wup, OutputFormat.Loadiine };
    /// <summary>
    /// Output line of the review summary.
    /// </summary>
    public string ReviewOutput => Format == OutputFormat.Loadiine
        ? "Loadiine folder (code, content, meta) → SD:/wiiu/games"
        : "WUP install package → SD:/install";
    /// <summary>
    /// Game line of the review summary.
    /// </summary>
    public string ReviewGame
    {
        get
        {
            var parts = new List<string> { string.IsNullOrWhiteSpace(Name) ? "Not named" : Name!.Trim().Replace(",", " / ") };
            if (!string.IsNullOrWhiteSpace(ShortName))
                parts.Add("icon: " + ShortName!.Trim());
            if (ClearedProductId(ProductId) is { } id)
                parts.Add("#" + id);
            if (IsGamePadVisible && GamePad)
                parts.Add("GamePad controller");
            return string.Join(" \u00b7 ", parts);
        }
    }

    /// <summary>
    /// ROM line of the review summary.
    /// </summary>
    public string ReviewRom => string.IsNullOrWhiteSpace(RomPath) ? "Not selected" : RomPath!;

    /// <summary>
    /// File types the selected console accepts, for the ROM hint.
    /// </summary>
    public string RomExtensions => string.Join(", ", RomFilters(SelectedConsole)[0].Patterns.Select(p => p.TrimStart('*')));

    /// <summary>
    /// Label of the selected console.
    /// </summary>
    public string SelectedConsoleName => Assets.ConsoleIcons.DisplayName(SelectedConsole);

    /// <summary>
    /// The step on screen as a settable item, for the step dots; setting jumps there.
    /// </summary>
    public WizardStep? SelectedStep
    {
        get => CurrentWizardStep;
        set
        {
            if (value is not null)
                Step = value.Number;
        }
    }

    /// <summary>
    /// True when the product ID is blank or exactly four characters.
    /// </summary>
    private bool IsBlank =>
        string.IsNullOrWhiteSpace(RomPath) && string.IsNullOrWhiteSpace(Name) && string.IsNullOrWhiteSpace(ShortName) && string.IsNullOrWhiteSpace(ProductId)
        && !Icon.HasPath && !BootTv.HasPath && !BootDrc.HasPath && !BootLogo.HasPath && !BootSound.HasPath;

    private bool IsProductIdValid => ClearedProductId(ProductId) is null or { Length: 4 };

    /// <inheritdoc/>
    public override Task ActivateAsync()
    {
        Refresh();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Fills every step from an earlier inject, keeps its IDs so the rebuild replaces it, and lands on Review.
    /// </summary>
    /// <param name="record">Inject to build again.</param>
    public void Load(InjectionRecord record)
    {
        if (record is null)
            throw new ArgumentNullException(nameof(record));

        StartOver();
        SelectedConsole = record.Console;
        SelectedBase = Bases.FirstOrDefault(b => b.Base.TitleId.Equals(record.BaseTitleId)) ?? SelectedBase;
        RomPath = record.RomPath;
        Name = record.Name;
        ShortName = record.ShortName;
        ProductId = record.ProductId;
        GamePad = record.GamePad;
        Format = record.Format;
        Icon.Path = record.Artwork.Icon;
        BootTv.Path = record.Artwork.BootTv;
        BootDrc.Path = record.Artwork.BootDrc;
        BootLogo.Path = record.Artwork.BootLogo;
        BootSound.Path = record.BootSoundPath;
        CurrentOptions.Load(record.Options);
        _identity = record.Identity;
        Step = Steps.Count;
    }

    /// <summary>
    /// Clears every field and returns to the first step, ready for the next title.
    /// </summary>
    public void StartOver()
    {
        _identity = null;
        RomPath = null;
        Name = null;
        ShortName = null;
        ProductId = null;
        GamePad = false;
        Format = OutputFormat.Wup;
        Icon.Path = null;
        BootTv.Path = null;
        BootDrc.Path = null;
        BootLogo.Path = null;
        BootSound.Path = null;
        ArtworkBuilder.Clear();
        Log.Clear();
        Status = null;
        CurrentStep = null;
        SelectedConsole = SourceConsole.Nes;
        CurrentOptions = CreateOptions(SelectedConsole);
        ArtworkBuilder.Refresh(SelectedConsole, Name, ShortName);
        Refresh();
        Step = 1;
    }

    private static string? ClearedProductId(string? productId) =>
        string.IsNullOrWhiteSpace(productId) ? null : productId.Trim();

    private static void DeleteWork(string work)
    {
        try
        {
            if (Directory.Exists(work))
                Directory.Delete(work, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static FileFilter[] RomFilters(SourceConsole console) => console switch
    {
        SourceConsole.Nes => new FileFilter[] { new("NES ROMs", "*.nes") },
        SourceConsole.Snes => new FileFilter[] { new("SNES ROMs", "*.sfc", "*.smc") },
        SourceConsole.N64 => new FileFilter[] { new("Nintendo 64 ROMs", "*.z64", "*.n64", "*.v64") },
        SourceConsole.Gba => new FileFilter[] { new("Game Boy ROMs", "*.gba", "*.gb", "*.gbc", "*.sgb") },
        SourceConsole.Nds => new FileFilter[] { new("Nintendo DS ROMs", "*.nds") },
        SourceConsole.Tg16 => new FileFilter[] { new("TurboGrafx-16 ROMs", "*.pce") },
        SourceConsole.Msx => new FileFilter[] { new("MSX ROMs", "*.rom", "*.mx1", "*.mx2") },
        SourceConsole.Wii => new FileFilter[]
        {
            new("Wii images, homebrew and channels", "*.iso", "*.wbfs", "*.dol", "*.wad"),
            new("Disc images", "*.iso", "*.wbfs"),
            new("Homebrew", "*.dol"),
            new("Channels", "*.wad"),
        },
        SourceConsole.GameCube => new FileFilter[] { new("GameCube images", "*.iso", "*.gcm", "*.gcz") },
        _ => throw new ArgumentOutOfRangeException(nameof(console), console, "Unknown console."),
    };

    private static (InjectionWarning Warning, string Message)? WarningFor(SourceConsole console, string romPath) => console switch
    {
        SourceConsole.Nds => (InjectionWarning.NdsDsiEnhanced, NdsWarning),
        SourceConsole.Snes => (InjectionWarning.SnesCoProcessor, SnesWarning),
        SourceConsole.GameCube when string.Equals(Path.GetExtension(romPath), ".gcz", StringComparison.OrdinalIgnoreCase) => (InjectionWarning.GameCubeGcz, GczWarning),
        _ => null,
    };

    private Injection BuildInjection() =>
        new(SelectedBase!.Base, new Rom(RomPath!, SelectedConsole), GameFactory.Create(Name!, ShortName, ClearedProductId(ProductId), IsGamePadVisible && GamePad, identity: _identity))
        {
            Artwork = new Artwork { Icon = Icon.Path, BootTv = BootTv.Path, BootDrc = BootDrc.Path, BootLogo = BootLogo.Path },
            BootSoundPath = BootSound.Path,
            Format = Format,
            Options = CurrentOptions.Build(),
        };

    /// <summary>
    /// Stops the running inject.
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsRunning))]
    private void Cancel() => _cancellation?.Cancel();

    /// <summary>
    /// Forgets the picked ROM.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasRom))]
    private void ClearRom() => RomPath = null;

    /// <summary>
    /// Asks the user first when the inject may not work; false means stop.
    /// </summary>
    private async Task<bool> ConfirmWarningAsync()
    {
        if (WarningFor(SelectedConsole, RomPath!) is not { } warning || _settings.Current.IsSuppressed(warning.Warning))
            return true;

        return await _dialogs.ConfirmAsync(WarningTitle, warning.Message).ConfigureAwait(true);
    }

    /// <summary>
    /// Jumps to a wizard step.
    /// </summary>
    /// <param name="step">Step to show.</param>
    [RelayCommand]
    private void GoToStep(WizardStep? step)
    {
        if (step is not null)
            Step = step.Number;
    }

    private ConsoleOptionsViewModel CreateOptions(SourceConsole console) => console switch
    {
        SourceConsole.Nes => new NesOptionsViewModel(),
        SourceConsole.Snes => new SnesOptionsViewModel(),
        SourceConsole.N64 => new N64OptionsViewModel(_dialogs, () => _settings.WorkPath),
        SourceConsole.Gba => new GbaOptionsViewModel(),
        SourceConsole.Nds => new NdsOptionsViewModel(_dialogs),
        SourceConsole.Wii => new WiiOptionsViewModel(_dialogs),
        SourceConsole.GameCube => new GameCubeOptionsViewModel(_dialogs),
        _ => new NoOptionsViewModel(console),
    };

    /// <summary>
    /// Confirms any warning, builds the injection and runs it, reporting the outcome.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanInject))]
    private async Task InjectAsync()
    {
        if (!await ConfirmWarningAsync().ConfigureAwait(true))
            return;

        Injection injection;
        try
        {
            injection = BuildInjection();
        }
        catch (ArgumentException e)
        {
            await _dialogs.ShowErrorAsync(DialogTitle, e.Message).ConfigureAwait(true);
            return;
        }

        var work = Path.Combine(_settings.WorkPath, Guid.NewGuid().ToString("N"));
        using var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        IsRunning = true;
        Log.Clear();
        CurrentStep = null;
        Status = "Running";
        var succeeded = false;
        try
        {
            var service = _injections.Create();
            var progress = new Progress<InjectionProgress>(Report);
            var copying = new Progress<string>(file => Log.Add("Copying " + file));
            var token = cancellation.Token;
            var result = await Task.Run(() => service.InjectAsync(injection, work, _settings.OutputPath, progress, token), token).ConfigureAwait(true);
            var copied = await CopyToCardAsync(result.OutputDirectory, copying, token).ConfigureAwait(true);
            await RememberAsync(injection, result).ConfigureAwait(true);
            Status = "Done";
            await _dialogs.ShowInfoAsync(DialogTitle, $"Title written to {copied ?? result.OutputDirectory}").ConfigureAwait(true);
            succeeded = true;
        }
        catch (OperationCanceledException)
        {
            Status = "Cancelled";
        }
        catch (InjectionException e)
        {
            Status = "Failed";
            await _dialogs.ShowErrorAsync(DialogTitle, $"Failed at {e.Step}: {e.Message}").ConfigureAwait(true);
        }
        catch (Exception e)
        {
            Status = "Failed";
            await _dialogs.ShowErrorAsync(DialogTitle, e.Message).ConfigureAwait(true);
        }
        finally
        {
            _cancellation = null;
            IsRunning = false;
            DeleteWork(work);
        }

        if (succeeded)
            StartOver();
    }

    /// <summary>
    /// Copies the packed title onto the SD card when that setting is on; returns where it landed, or null.
    /// </summary>
    /// <param name="titleDirectory">Folder holding the packed title.</param>
    /// <param name="progress">File names as they are copied.</param>
    /// <param name="cancellationToken">Stops the copy.</param>
    private async Task<string?> CopyToCardAsync(string titleDirectory, IProgress<string> progress, CancellationToken cancellationToken)
    {
        if (!_settings.Current.CopyToSdCard || string.IsNullOrWhiteSpace(_settings.SdPath))
            return null;

        CurrentStep = "Copying to the SD card";
        try
        {
            return await _sdCard.CopyAsync(titleDirectory, _settings.SdPath, progress, cancellationToken).ConfigureAwait(true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
        {
            await _dialogs.ShowErrorAsync(DialogTitle, "The title was packed, but copying it to the SD card failed: " + e.Message).ConfigureAwait(true);
            return null;
        }
    }

    /// <summary>
    /// The artwork field a slot's image lands in.
    /// </summary>
    /// <param name="slot">Slot that was built.</param>
    /// <summary>
    /// Adds the finished inject to the history; a failure there is reported but does not fail the inject.
    /// </summary>
    /// <param name="injection">What was injected.</param>
    /// <param name="result">What came out.</param>
    private async Task RememberAsync(Injection injection, InjectedTitle result)
    {
        var record = new InjectionRecord(Guid.NewGuid().ToString("N"), DateTimeOffset.Now, injection.Console, injection.Base.TitleId, injection.Rom.Path, Name!.Trim(), TitleIdentity.Of(result.Game))
        {
            Artwork = injection.Artwork,
            BootSoundPath = injection.BootSoundPath,
            Format = injection.Format,
            GamePad = IsGamePadVisible && GamePad,
            Options = injection.Options,
            OutputDirectory = result.OutputDirectory,
            ProductId = ClearedProductId(ProductId),
            ShortName = string.IsNullOrWhiteSpace(ShortName) ? null : ShortName!.Trim(),
        };
        try
        {
            _history.Add(record, result.IconTga);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            await _dialogs.ShowErrorAsync(DialogTitle, "The title was written, but it could not be added to the history: " + e.Message).ConfigureAwait(true);
        }
    }

    private PathFieldViewModel SlotFor(ImageSlot slot) =>
        slot == ImageSlot.Icon ? Icon
        : slot == ImageSlot.BootTv ? BootTv
        : slot == ImageSlot.BootDrc ? BootDrc
        : BootLogo;

    /// <summary>
    /// Shows the Bases and Keys page.
    /// </summary>
    [RelayCommand]
    private void ManageBases() => _navigation.Show<BasesViewModel>();

    /// <summary>
    /// Opens the community compatibility list for the selected console.
    /// </summary>
    [RelayCommand]
    private Task OpenCompatibilityListAsync() => _compatibility.OpenAsync(SelectedConsole);

    /// <summary>
    /// Plays the chosen boot sound as the console will, or stops it when it is already playing.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanPreviewSound))]
    private async Task PreviewSoundAsync()
    {
        if (IsPlayingSound)
        {
            _sounds.Stop();
            return;
        }

        try
        {
            _sounds.Play(BootSound.Path!);
            IsPlayingSound = true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or NotSupportedException or InvalidOperationException)
        {
            IsPlayingSound = false;
            await _dialogs.ShowErrorAsync("Boot sound", e.Message).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Advances one step.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void NextStep() => Step++;

    /// <summary>
    /// Clears the wizard after asking, unless it is already blank.
    /// </summary>
    [RelayCommand]
    private async Task StartOverAsync()
    {
        if (IsRunning)
            return;
        if (!IsBlank && !await _dialogs.ConfirmAsync(DialogTitle, "Clear every field and go back to the first step?").ConfigureAwait(true))
            return;

        StartOver();
    }

    /// <summary>
    /// True when the ROM will not fit the base.
    /// </summary>
    public bool HasRomFitHint => RomFitHint is not null;
    /// <summary>
    /// The check that the ROM fits the base, for awaiting; a finished task when none is running.
    /// </summary>
    public Task RomFitCheck { get; private set; } = Task.CompletedTask;

    partial void OnSelectedBaseChanged(BaseChoice? value)
    {
        if (value is { IsUsable: true } && Step == 2)
            Step = 3;
        RomFitCheck = CheckRomFitAsync();
    }

    /// <summary>
    /// Back to the base step, to pick a bigger one.
    /// </summary>
    [RelayCommand]
    private void ChangeBase() => Step = 2;

    /// <summary>
    /// Asks whether the ROM fits the base off the UI thread and shows why when it does not.
    /// </summary>
    private async Task CheckRomFitAsync()
    {
        var @base = SelectedBase;
        var rom = RomPath;
        if (@base is not { IsPresent: true } || string.IsNullOrWhiteSpace(rom))
        {
            RomFitHint = null;
            return;
        }
        var hint = await Task.Run(() => _injections.RomFit(@base.Base, rom!)).ConfigureAwait(true);
        // a later change wins
        if (ReferenceEquals(SelectedBase, @base) && RomPath == rom)
            RomFitHint = hint;
    }

    partial void OnNameChanged(string? value) => ArtworkBuilder.Refresh(SelectedConsole, value, ShortName);

    partial void OnRomPathChanged(string? value)
    {
        MissingKeys = _injections.MissingKeys(SelectedConsole, value);
        RomFitCheck = CheckRomFitAsync();
        CommunityArtwork.Reset();
        if (value is not null && string.IsNullOrWhiteSpace(Name) && SuggestedName(value) is { } suggested)
            Name = suggested;
    }

    /// <summary>
    /// The ROM's own header name, when it has one and can be read.
    /// </summary>
    private string? SuggestedName(string romPath)
    {
        try
        {
            return RomNames.Suggest(SelectedConsole, romPath);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// Fills the artwork slots, the boot sound and the N64 INI from a community folder; the GamePad screen only when the folder had one.
    /// </summary>
    private void TakeCommunityArtwork(CommunityArtworkFiles files)
    {
        Icon.Path = files.Icon;
        BootTv.Path = files.BootTv;
        if (files.BootDrc is not null)
            BootDrc.Path = files.BootDrc;
        if (files.BootSound is not null)
            BootSound.Path = files.BootSound;
        if (files.GameIni is not null && CurrentOptions is N64OptionsViewModel n64)
            n64.Ini.Path = files.GameIni;
    }

    partial void OnShortNameChanged(string? value) => ArtworkBuilder.Refresh(SelectedConsole, Name, value);

    partial void OnSelectedConsoleChanged(SourceConsole value)
    {
        RomPath = null;
        CurrentOptions = CreateOptions(value);
        ArtworkBuilder.Refresh(value, Name, ShortName);
        Refresh();
        if (Step == 1)
            Step = 2;
    }

    /// <summary>
    /// Opens the ROM picker for the selected console.
    /// </summary>
    [RelayCommand]
    private async Task PickRomAsync()
    {
        var picked = await _dialogs.PickOpenFileAsync("ROM", RomFilters(SelectedConsole)).ConfigureAwait(true);
        if (picked is not null)
            RomPath = picked;
    }

    /// <summary>
    /// Goes back one step.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private void PreviousStep() => Step--;

    /// <summary>
    /// Opens a folder picker for a TurboCD game.
    /// </summary>
    [RelayCommand]
    private async Task PickTurboCdFolderAsync()
    {
        var picked = await _dialogs.PickFolderAsync("TurboCD folder").ConfigureAwait(true);
        if (picked is not null)
            RomPath = picked;
    }

    /// <summary>
    /// Rebuilds the base list and missing keys for the selected console, keeping the selection where it survives.
    /// </summary>
    private void Refresh()
    {
        var previous = SelectedBase?.Base.TitleId;
        MissingKeys = _injections.MissingKeys(SelectedConsole, RomPath);
        var step = Step;
        Bases.Clear();
        foreach (var @base in _bases.Available(SelectedConsole))
            Bases.Add(new BaseChoice(@base, _bases.Status(@base), MissingKeys.Count == 0, _bases.HasTitleKey(@base)));

        SelectedBase = Bases.FirstOrDefault(b => previous is { } id && b.Base.TitleId.Equals(id))
                       ?? Bases.FirstOrDefault(b => b.IsPresent && b.Base.IsRecommended)
                       ?? Bases.FirstOrDefault(b => b.IsPresent)
                       ?? Bases.FirstOrDefault(b => b.Base.IsRecommended)
                       ?? Bases.FirstOrDefault();
        Step = step;
    }

    private void Report(InjectionProgress progress)
    {
        CurrentStep = progress.Step.ToString();
        Log.Add($"{progress.Step}: {progress.Message}");
    }
}
