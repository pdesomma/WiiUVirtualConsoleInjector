using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Infrastructure;
using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// The bases and keys page: the user's keys, and the bases for one console.
/// </summary>
public sealed partial class BasesViewModel : PageViewModel
{
    private const int KeySize = 16;

    private readonly IBaseService _bases;
    private readonly IDialogService _dialogs;
    private readonly IInjectionServiceFactory _injections;
    private readonly IKeyStore _keys;
    private readonly List<BaseRowViewModel> _rows = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CustomFolderHint), nameof(HasCustomFolder))]
    private string? _customFolder;
    [ObservableProperty]
    private BaseFolderKind? _customFolderKind;
    [ObservableProperty]
    private string _customName = string.Empty;
    [ObservableProperty]
    private string? _customProgress;
    [ObservableProperty]
    private Region _customRegion = Region.UnitedStates;
    [ObservableProperty]
    private string _customTitleId = string.Empty;
    [ObservableProperty]
    private string _filter = string.Empty;
    [ObservableProperty]
    private SourceConsole _selectedConsole;

    /// <summary>
    /// Creates a new instance of the <see cref="BasesViewModel"/> class.
    /// </summary>
    /// <param name="bases">Catalog, status and download.</param>
    /// <param name="keys">The user's keys.</param>
    /// <param name="injections">Builds the service that inspects.</param>
    /// <param name="dialogs">Pickers and messages.</param>
    public BasesViewModel(IBaseService bases, IKeyStore keys, IInjectionServiceFactory injections, IDialogService dialogs)
        : base("Bases & Keys", "wiiu.png", "M12 8 A4 4 0 1 1 4 8 A4 4 0 1 1 12 8 M10.8 10.8L20 20 M15.5 15.5l2.2 -2.2 M18 18l2.2 -2.2")
    {
        _bases = bases ?? throw new ArgumentNullException(nameof(bases));
        _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        _injections = injections ?? throw new ArgumentNullException(nameof(injections));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        Bases = new ObservableCollection<BaseRowViewModel>();

        WiiUCommonKeyEntry = new KeyEntryViewModel(
            InjectionServiceFactory.WiiUCommonKeyName,
            () => _keys.CommonKey?.ToString(),
            hex => _keys.CommonKey = new WiiUSharp.Nus.CommonKey(KeyHex.Parse(hex, KeySize)),
            () => _keys.CommonKey = null,
            _dialogs,
            RefreshStatuses);
        WiiCommonKeyEntry = new KeyEntryViewModel(
            InjectionServiceFactory.WiiCommonKeyName,
            () => _keys.WiiCommonKey is { } key ? KeyHex.Format(key.ToArray()) : null,
            hex => _keys.WiiCommonKey = new WiiSharp.CommonKey(KeyHex.Parse(hex, KeySize)),
            () => _keys.WiiCommonKey = null,
            _dialogs,
            RefreshStatuses);
        // consoles that run a bundled RetroArch core instead of a base have nothing to manage here
        Consoles = Enum.GetValues<SourceConsole>().Where(c => _bases.Available(c).Count > 0).ToArray();
        Regions = Enum.GetValues<Region>();
        LoadBases();
    }

    /// <summary>
    /// Re-reads keys and base statuses after the previous application's data was taken.
    /// </summary>
    /// <param name="legacy">Raises the import event.</param>
    public void FollowImports(LegacyImportViewModel legacy)
    {
        if (legacy is null)
            throw new ArgumentNullException(nameof(legacy));
        legacy.Imported += (_, _) => RefreshStatuses();
    }

    /// <summary>
    /// Rows for the selected console that match <see cref="Filter"/>, custom ones last.
    /// </summary>
    public ObservableCollection<BaseRowViewModel> Bases { get; }
    /// <summary>
    /// Consoles to choose from.
    /// </summary>
    public IReadOnlyList<SourceConsole> Consoles { get; }
    /// <summary>
    /// Regions to choose from for a custom base.
    /// </summary>
    public IReadOnlyList<Region> Regions { get; }
    /// <summary>
    /// The Wii common key.
    /// </summary>
    public KeyEntryViewModel WiiCommonKeyEntry { get; }
    /// <summary>
    /// The Wii U common key.
    /// </summary>
    public KeyEntryViewModel WiiUCommonKeyEntry { get; }

    /// <inheritdoc/>
    public override Task ActivateAsync()
    {
        WiiUCommonKeyEntry.Refresh();
        WiiCommonKeyEntry.Refresh();
        RefreshStatuses();
        return Task.CompletedTask;
    }

    /// <summary>
    /// What the picked folder turned out to be, for the form.
    /// </summary>
    public string? CustomFolderHint => CustomFolder is null ? null : CustomFolderKind switch
    {
        BaseFolderKind.Package => "An installable package; it will be unpacked with the Wii U common key.",
        BaseFolderKind.Title => "A title folder (code, content, meta); it will be copied into the base store.",
        _ => "Not a base: neither code/content/meta nor a title.tmd inside.",
    };
    /// <summary>
    /// True once a folder is picked.
    /// </summary>
    public bool HasCustomFolder => CustomFolder is not null;

    /// <summary>
    /// Remembers a base the catalog does not list; imports its folder first when one was picked.
    /// </summary>
    [RelayCommand]
    private async Task AddCustomBaseAsync()
    {
        if (!TitleId.TryParse(CustomTitleId.Trim(), out var titleId))
        {
            await _dialogs.ShowErrorAsync("Custom base", "Title ID must be sixteen hex digits.");
            return;
        }
        if (string.IsNullOrWhiteSpace(CustomName))
        {
            await _dialogs.ShowErrorAsync("Custom base", "Name is required.");
            return;
        }
        if (_rows.Any(b => b.Base.TitleId.Equals(titleId) && !b.IsCustom))
        {
            await _dialogs.ShowErrorAsync("Custom base", $"Title {titleId} is already in the catalog.");
            return;
        }
        if (CustomFolder is not null && CustomFolderKind is null)
        {
            await _dialogs.ShowErrorAsync("Custom base", "The picked folder is not a base.");
            return;
        }

        var title = new BaseTitle(titleId, CustomName.Trim(), CustomRegion, SelectedConsole) { IsCustom = true };
        if (CustomFolder is not null)
        {
            try
            {
                await _bases.ImportAsync(title, CustomFolder, new Progress<string>(m => CustomProgress = m));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
            {
                CustomProgress = null;
                await _dialogs.ShowErrorAsync("Custom base", e.Message);
                return;
            }
        }
        _bases.AddCustom(title);
        CustomProgress = null;
        CustomTitleId = string.Empty;
        CustomName = string.Empty;
        CustomFolder = null;
        CustomFolderKind = null;
        LoadBases();
    }

    /// <summary>
    /// Picks the folder a custom base comes from and fills the form from what is inside it.
    /// </summary>
    [RelayCommand]
    private async Task PickCustomFolderAsync()
    {
        var folder = await _dialogs.PickFolderAsync("Base folder");
        if (folder is null)
            return;

        CustomFolder = folder;
        var info = BaseFolder.Inspect(folder);
        CustomFolderKind = info?.Kind;
        if (info?.TitleId is { } id)
            CustomTitleId = id.ToString();
        if (info?.Name is { } name)
            CustomName = name;
        if (info?.Region is { } region)
            CustomRegion = region;
    }

    /// <summary>
    /// Drops the picked folder; the base will be registered only.
    /// </summary>
    [RelayCommand]
    private void ClearCustomFolder()
    {
        CustomFolder = null;
        CustomFolderKind = null;
    }

    /// <summary>
    /// Forgets a custom base after confirming; its files stay in the store.
    /// </summary>
    /// <param name="row">The row to forget.</param>
    [RelayCommand]
    private async Task RemoveCustomBaseAsync(BaseRowViewModel? row)
    {
        if (row is null || !row.IsCustom)
            return;
        if (!await _dialogs.ConfirmAsync("Forget base", $"Forget {row.Name} [{row.Region}]? Its files stay in the base store."))
            return;

        _bases.RemoveCustom(row.Base.TitleId);
        LoadBases();
    }

    /// <summary>
    /// Shows the rows whose name, title ID or region contains the filter; all of them when it is blank.
    /// </summary>
    private void ApplyFilter()
    {
        var filter = Filter.Trim();
        Bases.Clear();
        foreach (var row in _rows.Where(r => Matches(r, filter)))
            Bases.Add(row);
    }

    /// <summary>
    /// Reads the Wii U common key out of an otp.bin the user picks.
    /// </summary>
    [RelayCommand]
    private async Task ImportOtpAsync()
    {
        var path = await _dialogs.PickOpenFileAsync("Read otp.bin", new FileFilter("otp.bin", "otp.bin"), new FileFilter("All files", "*"));
        if (path is null)
            return;

        try
        {
            _keys.CommonKey = OtpFile.ReadCommonKey(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
        {
            await _dialogs.ShowErrorAsync("Read otp.bin", e.Message);
            return;
        }

        WiiUCommonKeyEntry.Refresh();
        RefreshStatuses();
    }

    /// <summary>
    /// Rebuilds the rows for the selected console.
    /// </summary>
    private void LoadBases()
    {
        _rows.Clear();
        foreach (var title in _bases.Available(SelectedConsole))
            _rows.Add(NewRow(title, title.IsCustom));
        ApplyFilter();
    }

    /// <summary>
    /// Case-insensitive substring match against the row's name, title ID and region.
    /// </summary>
    /// <param name="row">Row to test.</param>
    /// <param name="filter">Trimmed filter text.</param>
    private static bool Matches(BaseRowViewModel row, string filter) =>
        filter.Length == 0
        || row.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
        || row.TitleId.Contains(filter, StringComparison.OrdinalIgnoreCase)
        || row.Region.Contains(filter, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// A row wired to this page's services.
    /// </summary>
    /// <param name="title">Base the row shows.</param>
    /// <param name="isCustom">True when the user added it.</param>
    private BaseRowViewModel NewRow(BaseTitle title, bool isCustom) => new(title, isCustom, _bases, _keys, _injections, _dialogs);

    partial void OnFilterChanged(string value) => ApplyFilter();

    partial void OnSelectedConsoleChanged(SourceConsole value) => LoadBases();

    /// <summary>
    /// Re-reads every row's status after keys or the store changed.
    /// </summary>
    private void RefreshStatuses()
    {
        foreach (var row in _rows)
            row.Refresh();
    }
}
