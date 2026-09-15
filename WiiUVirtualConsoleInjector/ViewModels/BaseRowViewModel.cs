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
    [NotifyPropertyChangedFor(nameof(CanDownload), nameof(CanInspect), nameof(DownloadHint), nameof(InspectHint), nameof(IsPresent), nameof(NeedsKey), nameof(StatusText))]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand), nameof(InspectCommand))]
    private BaseStatus _status;
    private bool _syncing;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTitleKeyValid))]
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
    /// Why the download is off, or what it will do.
    /// </summary>
    public string DownloadHint => Status switch
    {
        BaseStatus.Present => "This base is already in the base store.",
        BaseStatus.NeedsCommonKey => "Add the Wii U common key above first.",
        BaseStatus.NeedsTitleKey => "Add this base's title key first.",
        _ => "Downloads and decrypts this base.",
    };
    /// <summary>
    /// True when the user added it this session.
    /// </summary>
    public bool IsCustom { get; }
    /// <summary>
    /// Why the inspection is off, or what it will do.
    /// </summary>
    public string InspectHint => IsPresent ? "Checks the stored base for missing files." : "Download this base first.";
    /// <summary>
    /// True when the base is in the store.
    /// </summary>
    public bool IsPresent => Status == BaseStatus.Present;
    /// <summary>
    /// True when the title key text is 32 hex characters.
    /// </summary>
    public bool IsTitleKeyValid => HexKeyText.IsValid(TitleKey, TitleKeySize);
    /// <summary>
    /// Display name.
    /// </summary>
    public string Name => Base.Name;
    /// <summary>
    /// True when a common or title key is missing.
    /// </summary>
    public bool NeedsKey => Status is BaseStatus.NeedsCommonKey or BaseStatus.NeedsTitleKey;
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
        _syncing = true;
        try
        {
            Status = _bases.Status(Base);
            TitleKey = _keys.GetTitleKey(Base.TitleId)?.ToString() ?? string.Empty;
        }
        finally
        {
            _syncing = false;
        }
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
    /// Stores as soon as the hex is complete; emptying forgets the key.
    /// </summary>
    /// <param name="value">What the user typed.</param>
    partial void OnTitleKeyChanged(string value)
    {
        if (_syncing)
            return;

        if (string.IsNullOrWhiteSpace(value) ? _keys.GetTitleKey(Base.TitleId) is not null : IsTitleKeyValid)
            StoreTitleKey();
    }

    /// <summary>
    /// Stores the title key text; empty clears it, invalid hex is reported.
    /// </summary>
    [RelayCommand]
    private async Task SaveTitleKeyAsync()
    {
        if (StoreTitleKey() is { } error)
            await _dialogs.ShowErrorAsync(Base.ToString(), error.Message);
    }

    /// <summary>
    /// Stores the title key text, empty clearing it; returns the format error, or null once stored.
    /// </summary>
    private FormatException? StoreTitleKey()
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
                return e;
            }
        }

        _keys.SetTitleKey(Base.TitleId, key);
        Refresh();
        return null;
    }
}
