using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp.Wud;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// Game backups onto the SD card: an installable package folder is copied under install as it is; a disc dump is unpacked into packages first.
/// </summary>
public sealed partial class BackupsViewModel : PageViewModel
{
    private readonly IDialogService _dialogs;
    private readonly IDiscBackup _discs;
    private readonly IKeyStore _keys;
    private readonly ISdCard _sdCard;
    private readonly ISettingsService _settings;

    private CancellationTokenSource? _cancel;

    [ObservableProperty]
    private string? _discKeyHex;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImage), nameof(NeedsDiscKey), nameof(SourceStatus))]
    [NotifyCanExecuteChangedFor(nameof(PushCommand))]
    private string? _imagePath;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PushCommand), nameof(CancelCommand))]
    private bool _isRunning;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPackage), nameof(SourceStatus), nameof(PackageSize), nameof(HasPackageSize))]
    [NotifyCanExecuteChangedFor(nameof(PushCommand))]
    private WupPackage? _package;
    [ObservableProperty]
    private string? _progress;

    /// <summary>
    /// Creates a new instance of the <see cref="BackupsViewModel"/> class.
    /// </summary>
    /// <param name="dialogs">Pickers and error boxes.</param>
    /// <param name="settings">Card and output folders.</param>
    /// <param name="sdCard">Copies onto the card.</param>
    /// <param name="keys">The Wii U common key a dump needs.</param>
    /// <param name="discs">Unpacks dumps.</param>
    public BackupsViewModel(IDialogService dialogs, ISettingsService settings, ISdCard sdCard, IKeyStore keys, IDiscBackup discs)
        : base("Backups", "wiiu.png")
    {
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _sdCard = sdCard ?? throw new ArgumentNullException(nameof(sdCard));
        _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        _discs = discs ?? throw new ArgumentNullException(nameof(discs));
    }

    /// <summary>
    /// Where the card's install folder is, or why there is none.
    /// </summary>
    public string CardStatus => HasCard ? Path.Combine(_settings.SdPath, SdCard.InstallFolder) : "No SD card; pick one on Settings.";
    /// <summary>
    /// True when a card root is known.
    /// </summary>
    public bool HasCard => !string.IsNullOrWhiteSpace(_settings.SdPath);
    /// <summary>
    /// True when the Wii U common key is stored.
    /// </summary>
    public bool HasCommonKey => _keys.CommonKey is not null;
    /// <summary>
    /// True when a dump is picked.
    /// </summary>
    public bool HasImage => ImagePath is not null;
    /// <summary>
    /// True when a package folder is picked.
    /// </summary>
    public bool HasPackage => Package is not null;
    /// <summary>
    /// What happened, newest last.
    /// </summary>
    public ObservableCollection<string> Log { get; } = new();
    /// <summary>
    /// True when a dump is picked with no game.key beside it.
    /// </summary>
    public bool NeedsDiscKey => ImagePath is { } path && DiscKey.Beside(path) is null;
    /// <summary>
    /// Packages already on the card's install folder.
    /// </summary>
    public ObservableCollection<WupPackage> OnCard { get; } = new();
    /// <summary>
    /// True when a complete package is picked.
    /// </summary>
    public bool HasPackageSize => PackageSize is not null;
    /// <summary>
    /// What the picked package takes; null unless one is picked and complete.
    /// </summary>
    public ByteSize? PackageSize => Package is { IsComplete: true } p ? new ByteSize(p.Size) : null;
    /// <summary>
    /// What the picked source is.
    /// </summary>
    public string SourceStatus => Package is { } p
        ? p.IsComplete ? $"{p.TitleId} v{p.TitleVersion}, {p.ContentCount} contents" : $"{p.TitleId}: missing {string.Join(", ", p.Missing)}"
        : ImagePath is { } i ? Path.GetFileName(i) + (NeedsDiscKey ? " (no game.key beside it; enter the disc key)" : "") : "Nothing picked.";

    /// <inheritdoc/>
    public override Task ActivateAsync()
    {
        OnPropertyChanged(nameof(HasCard));
        OnPropertyChanged(nameof(CardStatus));
        OnPropertyChanged(nameof(HasCommonKey));
        RefreshCard();
        PushCommand.NotifyCanExecuteChanged();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Re-reads what is under the card's install folder.
    /// </summary>
    public void RefreshCard()
    {
        OnCard.Clear();
        if (!HasCard)
            return;
        var install = Path.Combine(_settings.SdPath, SdCard.InstallFolder);
        if (!Directory.Exists(install))
            return;
        foreach (var folder in Directory.GetDirectories(install).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            if (!WupPackage.LooksLike(folder))
                continue;
            try
            {
                OnCard.Add(WupPackage.Inspect(folder));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException)
            {
            }
        }
    }

    private bool CanPush() => !IsRunning && HasCard && (Package is { IsComplete: true } || ImagePath is not null);

    /// <summary>
    /// Stops the copy between files.
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsRunning))]
    private void Cancel() => _cancel?.Cancel();

    /// <summary>
    /// Picks a .wud or .wux dump.
    /// </summary>
    [RelayCommand]
    private async Task PickImageAsync()
    {
        var picked = await _dialogs.PickOpenFileAsync("Disc dump", new FileFilter("Wii U disc dumps", "*.wud", "*.wux"));
        if (picked is null)
            return;
        if (!_discs.Accepts(picked))
        {
            await _dialogs.ShowErrorAsync("Backups", "That is not a .wud or .wux dump.");
            return;
        }
        Package = null;
        ImagePath = picked;
    }

    /// <summary>
    /// Picks an installable package folder and checks it.
    /// </summary>
    [RelayCommand]
    private async Task PickPackageAsync()
    {
        var picked = await _dialogs.PickFolderAsync("Package folder");
        if (picked is null)
            return;
        try
        {
            var package = WupPackage.Inspect(picked);
            ImagePath = null;
            Package = package;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            await _dialogs.ShowErrorAsync("Backups", e.Message);
        }
    }

    /// <summary>
    /// Copies the package, or unpacks the dump into the output folder and copies each package, onto the card.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanPush))]
    private async Task PushAsync()
    {
        IsRunning = true;
        Log.Clear();
        _cancel = new CancellationTokenSource();
        var progress = new Progress<string>(m => Progress = m);
        try
        {
            IReadOnlyList<string> folders;
            if (Package is { } package)
                folders = new[] { package.Folder };
            else
            {
                if (_keys.CommonKey is not { } commonKey)
                    throw new InvalidOperationException("A dump needs the Wii U common key; add it on Bases & Keys.");
                DiscKey? discKey = null;
                if (NeedsDiscKey)
                {
                    if (string.IsNullOrWhiteSpace(DiscKeyHex))
                        throw new InvalidOperationException("Enter the disc key, or put game.key beside the dump.");
                    discKey = DiscKey.Parse(DiscKeyHex!.Trim());
                }
                Log.Add("Unpacking " + Path.GetFileName(ImagePath!) + "...");
                folders = await _discs.UnpackAsync(ImagePath!, discKey, commonKey, _settings.OutputPath, progress, _cancel.Token);
                foreach (var folder in folders)
                    Log.Add("Unpacked to " + folder);
            }
            foreach (var folder in folders)
            {
                var landed = await _sdCard.CopyAsync(folder, _settings.SdPath, progress, _cancel.Token);
                Log.Add("Copied to " + landed);
            }
            Progress = null;
            Log.Add("Done. Install it with WUP Installer on the console.");
        }
        catch (OperationCanceledException)
        {
            Progress = null;
            Log.Add("Cancelled.");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException or FormatException or ArgumentException)
        {
            Progress = null;
            Log.Add("Failed: " + e.Message);
            await _dialogs.ShowErrorAsync("Backups", e.Message);
        }
        finally
        {
            _cancel.Dispose();
            _cancel = null;
            IsRunning = false;
            RefreshCard();
        }
    }
}
