using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Ports;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// Looks the picked ROM up in the community artwork repository and offers what it finds for the artwork slots.
/// </summary>
public sealed partial class CommunityArtworkViewModel : ViewModelBase
{
    private readonly ICommunityArtwork _artwork;
    private readonly Func<SourceConsole> _console;
    private readonly Func<string?> _rom;
    private readonly Func<string> _workFolder;
    private CancellationTokenSource? _lookup;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasHit))]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand), nameof(DismissCommand))]
    private CommunityArtworkFiles? _files;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LookUpCommand))]
    private bool _isBusy;
    [ObservableProperty]
    private string? _status;

    /// <summary>
    /// Creates a new instance of the <see cref="CommunityArtworkViewModel"/> class.
    /// </summary>
    /// <param name="artwork">The repository.</param>
    /// <param name="console">Console of the picked ROM.</param>
    /// <param name="rom">The picked ROM, or null.</param>
    /// <param name="workFolder">Where downloads go.</param>
    public CommunityArtworkViewModel(ICommunityArtwork artwork, Func<SourceConsole> console, Func<string?> rom, Func<string> workFolder)
    {
        _artwork = artwork ?? throw new ArgumentNullException(nameof(artwork));
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _rom = rom ?? throw new ArgumentNullException(nameof(rom));
        _workFolder = workFolder ?? throw new ArgumentNullException(nameof(workFolder));
    }

    /// <summary>
    /// Raised when the user takes the found files.
    /// </summary>
    public event EventHandler<CommunityArtworkFiles>? Applied;

    /// <summary>
    /// True when a lookup found and downloaded artwork that is still on offer.
    /// </summary>
    public bool HasHit => Files is not null;

    /// <summary>
    /// Whether a lookup can start.
    /// </summary>
    public bool CanLookUp => !IsBusy && !string.IsNullOrWhiteSpace(_rom());

    /// <summary>
    /// Forgets any offer, e.g. when the ROM changes.
    /// </summary>
    public void Reset()
    {
        _lookup?.Cancel();
        Files = null;
        Status = null;
        LookUpCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Hands the offer to the wizard.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasHit))]
    private void Apply()
    {
        if (Files is not { } files)
            return;
        Applied?.Invoke(this, files);
        Files = null;
        Status = $"Artwork from {files.Id} is in the slots below.";
    }

    /// <summary>
    /// Declines the offer.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasHit))]
    private void Dismiss()
    {
        Files = null;
        Status = null;
    }

    /// <summary>
    /// Derives the repository ids for the ROM, asks the repository, and downloads a hit.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanLookUp))]
    private async Task LookUpAsync()
    {
        var rom = _rom();
        if (string.IsNullOrWhiteSpace(rom))
            return;

        _lookup?.Cancel();
        var lookup = _lookup = new CancellationTokenSource();
        IsBusy = true;
        Files = null;
        Status = "Looking…";
        try
        {
            var console = _console();
            IReadOnlyList<string> ids;
            try
            {
                ids = CommunityArtworkIds.Candidates(console, rom!);
            }
            catch (IOException error)
            {
                Status = "Could not read the ROM: " + error.Message;
                return;
            }
            if (ids.Count == 0)
            {
                Status = "This ROM carries no code the repository files games under.";
                return;
            }

            var hit = await _artwork.FindAsync(ids, lookup.Token).ConfigureAwait(true);
            if (hit is null)
            {
                Status = "Nothing in the community repository under " + string.Join(", ", ids) + ".";
                return;
            }

            var folder = Path.Combine(_workFolder(), "community-artwork", hit.Id.Replace('/', '_'));
            var files = new CommunityArtworkFiles(
                hit.Id,
                await Fetch(hit.Icon, folder, lookup.Token).ConfigureAwait(true),
                await Fetch(hit.BootTv, folder, lookup.Token).ConfigureAwait(true),
                hit.BootDrc is null ? null : await Fetch(hit.BootDrc, folder, lookup.Token).ConfigureAwait(true),
                hit.GameIni is null ? null : await Fetch(hit.GameIni, folder, lookup.Token).ConfigureAwait(true),
                hit.BootSound is null ? null : await Fetch(hit.BootSound, folder, lookup.Token).ConfigureAwait(true));
            if (lookup.IsCancellationRequested)
                return;
            Files = files;
            Status = $"Found under {hit.Id}" + Extras(files) + ".";
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception error) when (error is HttpRequestException or IOException)
        {
            Status = "The repository could not be reached: " + error.Message;
        }
        finally
        {
            if (ReferenceEquals(_lookup, lookup))
                IsBusy = false;
        }
    }

    private static string Extras(CommunityArtworkFiles files)
    {
        var extras = new List<string>();
        if (files.BootDrc is not null)
            extras.Add("a GamePad screen");
        if (files.GameIni is not null)
            extras.Add("an emulator INI");
        if (files.BootSound is not null)
            extras.Add("a boot sound");
        return extras.Count == 0 ? "" : ", with " + string.Join(", ", extras);
    }

    private async Task<string> Fetch(Uri source, string folder, CancellationToken cancellationToken)
    {
        var path = Path.Combine(folder, Path.GetFileName(source.LocalPath));
        await _artwork.DownloadAsync(source, path, cancellationToken).ConfigureAwait(true);
        return path;
    }
}
