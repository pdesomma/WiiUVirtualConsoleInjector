using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp.Nus;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// One base in the list: its status, title key, download and inspection.
/// </summary>
public sealed partial class BaseRowViewModel : ViewModelBase
{
    private const int TitleKeySize = 16;

    private readonly IBaseService _bases;
    private readonly IDialogService _dialogs;
    private readonly IInjectionServiceFactory _injections;
    private readonly IKeyStore _keys;

    [ObservableProperty]
    private bool _isTitleKeyRevealed;
    [ObservableProperty]
    private string _progressText = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDownload), nameof(CanInspect), nameof(StatusText))]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand), nameof(InspectCommand))]
    private BaseStatus _status;
    [ObservableProperty]
    private string _titleKey = string.Empty;

    /// <summary>
    /// Creates a new instance of the <see cref="BaseRowViewModel"/> class.
    /// </summary>
    /// <param name="base">Base this row shows.</param>
    /// <param name="isCustom">True when the user added it this session.</param>
    /// <param name="bases">Status and download.</param>
    /// <param name="keys">Where the title key lives.</param>
    /// <param name="injections">Builds the service that inspects.</param>
    /// <param name="dialogs">Messages to the user.</param>
    public BaseRowViewModel(BaseTitle @base, bool isCustom, IBaseService bases, IKeyStore keys, IInjectionServiceFactory injections, IDialogService dialogs)
    {
        Base = @base ?? throw new ArgumentNullException(nameof(@base));
        IsCustom = isCustom;
        _bases = bases ?? throw new ArgumentNullException(nameof(bases));
        _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        _injections = injections ?? throw new ArgumentNullException(nameof(injections));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        Refresh();
    }

    /// <summary>
    /// Base this row shows.
    /// </summary>
    public BaseTitle Base { get; }
    /// <summary>
    /// True when the keys are in hand and the base is not yet stored.
    /// </summary>
    public bool CanDownload => Status == BaseStatus.Downloadable;
    /// <summary>
    /// True when the base is in the store.
    /// </summary>
    public bool CanInspect => Status == BaseStatus.Present;
    /// <summary>
    /// True when the user added it this session.
    /// </summary>
    public bool IsCustom { get; }
    /// <summary>
    /// Display name.
    /// </summary>
    public string Name => Base.Name;
    /// <summary>
    /// Release region.
    /// </summary>
    public string Region => Base.Region.ToString();
    /// <summary>
    /// Status as shown to the user.
    /// </summary>
    public string StatusText => Status switch
    {
        BaseStatus.Present => "Present",
        BaseStatus.NeedsCommonKey => "Needs common key",
        BaseStatus.NeedsTitleKey => "Needs title key",
        BaseStatus.Downloadable => "Downloadable",
        _ => Status.ToString(),
    };
    /// <summary>
    /// Title ID as hex.
    /// </summary>
    public string TitleId => Base.TitleId.ToString();

    /// <summary>
    /// Re-reads status and the stored title key.
    /// </summary>
    public void Refresh()
    {
        Status = _bases.Status(Base);
        TitleKey = _keys.GetTitleKey(Base.TitleId)?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Shows one progress report.
    /// </summary>
    /// <param name="progress">Where the download is.</param>
    public void Report(BaseDownloadProgress progress)
    {
        if (progress is null)
            throw new ArgumentNullException(nameof(progress));

        ProgressText = Describe(progress);
    }

    /// <summary>
    /// Progress text for one report.
    /// </summary>
    /// <param name="progress">Where the download is.</param>
    private static string Describe(BaseDownloadProgress progress)
    {
        if (progress.Phase == BaseDownloadPhase.Unpacking)
            return $"Unpacking content {progress.Item}";

        var text = $"Downloading {progress.Item}";
        if (progress.ItemCount > 0)
            text += $" ({progress.ItemNumber}/{progress.ItemCount})";
        if (progress.BytesTotal is > 0)
            text += $" {progress.BytesReceived * 100 / progress.BytesTotal.Value}%";
        return text;
    }

    /// <summary>
    /// Fetches the base into the store, reporting progress; errors are shown, cancellation is not.
    /// </summary>
    /// <param name="cancellationToken">Stops the transfer.</param>
    [RelayCommand(CanExecute = nameof(CanDownload), IncludeCancelCommand = true)]
    private async Task DownloadAsync(CancellationToken cancellationToken)
    {
        ProgressText = "Starting";
        try
        {
            await _bases.DownloadAsync(Base, new Progress<BaseDownloadProgress>(Report), cancellationToken);
            ProgressText = "Downloaded";
        }
        catch (OperationCanceledException)
        {
            ProgressText = "Cancelled";
        }
        catch (InvalidDataException e)
        {
            ProgressText = "Failed";
            await _dialogs.ShowErrorAsync(Base.ToString(), $"The keys do not unlock this title; check the Wii U common key and the title key. {e.Message}");
        }
        catch (Exception e)
        {
            ProgressText = "Failed";
            await _dialogs.ShowErrorAsync(Base.ToString(), e.Message);
        }

        Refresh();
    }

    /// <summary>
    /// Checks the stored base and lists what it lacks.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanInspect))]
    private async Task InspectAsync()
    {
        IReadOnlyList<BaseIssue> issues;
        try
        {
            issues = await Task.Run(() => _injections.Create().InspectBase(Base));
        }
        catch (Exception e)
        {
            await _dialogs.ShowErrorAsync(Base.ToString(), e.Message);
            return;
        }

        await _dialogs.ShowInfoAsync(Base.ToString(), issues.Count == 0 ? "Base is usable" : string.Join(Environment.NewLine, issues));
    }

    /// <summary>
    /// Stores the title key text; empty clears it, invalid hex is reported.
    /// </summary>
    [RelayCommand]
    private async Task SaveTitleKeyAsync()
    {
        EncryptedTitleKey? key = null;
        if (!string.IsNullOrWhiteSpace(TitleKey))
        {
            try
            {
                key = new EncryptedTitleKey(KeyHex.Parse(TitleKey, TitleKeySize));
            }
            catch (FormatException e)
            {
                await _dialogs.ShowErrorAsync(Base.ToString(), e.Message);
                return;
            }
        }

        _keys.SetTitleKey(Base.TitleId, key);
        Refresh();
    }
}
