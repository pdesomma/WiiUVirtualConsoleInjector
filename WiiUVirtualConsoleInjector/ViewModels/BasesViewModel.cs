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
    private readonly List<BaseTitle> _customBases = new();
    private readonly IDialogService _dialogs;
    private readonly IInjectionServiceFactory _injections;
    private readonly IKeyStore _keys;

    [ObservableProperty]
    private string _customName = string.Empty;
    [ObservableProperty]
    private Region _customRegion = Region.UnitedStates;
    [ObservableProperty]
    private string _customTitleId = string.Empty;
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
        : base("Bases & Keys", "wiiu.png")
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
        AncastKeyEntry = new KeyEntryViewModel(
            "Ancast key",
            () => _keys.AncastKey?.ToString(),
            hex => _keys.AncastKey = AncastKey.Parse(hex),
            () => _keys.AncastKey = null,
            _dialogs,
            RefreshStatuses);
        Consoles = Enum.GetValues<SourceConsole>();
        Regions = Enum.GetValues<Region>();
        LoadBases();
    }

    /// <summary>
    /// The console's ancast key.
    /// </summary>
    public KeyEntryViewModel AncastKeyEntry { get; }
    /// <summary>
    /// Rows for the selected console, custom ones last.
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
        AncastKeyEntry.Refresh();
        RefreshStatuses();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds a base the catalog does not list, for this session.
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
        if (Bases.Any(b => b.Base.TitleId.Equals(titleId)))
        {
            await _dialogs.ShowErrorAsync("Custom base", $"Title {titleId} is already listed.");
            return;
        }

        var title = new BaseTitle(titleId, CustomName.Trim(), CustomRegion, SelectedConsole);
        _customBases.Add(title);
        Bases.Add(NewRow(title, isCustom: true));
        CustomTitleId = string.Empty;
        CustomName = string.Empty;
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
        Bases.Clear();
        foreach (var title in _bases.Available(SelectedConsole))
            Bases.Add(NewRow(title, isCustom: false));
        foreach (var title in _customBases.Where(t => t.Console == SelectedConsole))
            Bases.Add(NewRow(title, isCustom: true));
    }

    /// <summary>
    /// A row wired to this page's services.
    /// </summary>
    /// <param name="title">Base the row shows.</param>
    /// <param name="isCustom">True when the user added it.</param>
    private BaseRowViewModel NewRow(BaseTitle title, bool isCustom) => new(title, isCustom, _bases, _keys, _injections, _dialogs);

    partial void OnSelectedConsoleChanged(SourceConsole value) => LoadBases();

    /// <summary>
    /// Re-reads every row's status after keys or the store changed.
    /// </summary>
    private void RefreshStatuses()
    {
        foreach (var row in Bases)
            row.Refresh();
    }
}
