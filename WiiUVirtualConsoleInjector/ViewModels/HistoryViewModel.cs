using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Ports;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// Earlier injects as pages of Wii U menu tiles; picking one loads it back into the wizard.
/// </summary>
public sealed partial class HistoryViewModel : PageViewModel, IArrowNavigation
{
    /// <summary>
    /// Tiles per page: the Wii U menu's four columns by three rows.
    /// </summary>
    public const int PageSize = 12;

    private const string DialogTitle = "History";

    private readonly IDialogService _dialogs;
    private readonly IInjectionHistory _history;
    private readonly InjectViewModel _inject;
    private readonly INavigationService _navigation;
    private readonly IUiScheduler _ui;
    private IReadOnlyList<HistoryEntryViewModel> _entries = Array.Empty<HistoryEntryViewModel>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext), nameof(CanGoPrevious), nameof(PageLabel), nameof(SelectedPage))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand), nameof(PreviousPageCommand))]
    private int _page = 1;

    /// <summary>
    /// Creates a new instance of the <see cref="HistoryViewModel"/> class.
    /// </summary>
    /// <param name="history">Where injects are remembered.</param>
    /// <param name="inject">The wizard a record is loaded into.</param>
    /// <param name="navigation">Jumps to the wizard after a load.</param>
    /// <param name="dialogs">Confirmations and warnings.</param>
    /// <param name="ui">Marshals history changes onto the UI thread.</param>
    public HistoryViewModel(IInjectionHistory history, InjectViewModel inject, INavigationService navigation, IDialogService dialogs, IUiScheduler ui)
        : base("History", "help.png", "M12 8v4l2.5 2 M3.5 12a8.5 8.5 0 1 0 2.5-6 M3 4v3.5h3.5")
    {
        _history = history ?? throw new ArgumentNullException(nameof(history));
        _inject = inject ?? throw new ArgumentNullException(nameof(inject));
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        _history.Changed += (_, _) => _ui.Post(Reload);
        Reload();
    }

    /// <summary>
    /// True when a page follows.
    /// </summary>
    public bool CanGoNext => Page < PageCount;

    /// <summary>
    /// True when a page precedes.
    /// </summary>
    public bool CanGoPrevious => Page > 1;

    /// <summary>
    /// Tiles on the current page.
    /// </summary>
    public ObservableCollection<HistoryEntryViewModel> Current { get; } = new();

    /// <inheritdoc/>
    public System.Windows.Input.ICommand NextCommand => NextPageCommand;

    /// <inheritdoc/>
    public string NextHint => "Next page";

    /// <inheritdoc/>
    public System.Windows.Input.ICommand PreviousCommand => PreviousPageCommand;

    /// <inheritdoc/>
    public string PreviousHint => "Previous page";

    /// <summary>
    /// True when nothing has been injected yet.
    /// </summary>
    public bool IsEmpty => _entries.Count == 0;

    /// <summary>
    /// How many pages there are; never fewer than one.
    /// </summary>
    public int PageCount => Math.Max(1, (_entries.Count + PageSize - 1) / PageSize);

    /// <summary>
    /// "Page 2 of 3", or how many titles when there is one page.
    /// </summary>
    public string PageLabel => PageCount > 1
        ? $"Page {Page} of {PageCount}"
        : _entries.Count == 1 ? "1 title" : $"{_entries.Count} titles";

    /// <summary>
    /// One dot per page.
    /// </summary>
    public ObservableCollection<HistoryPage> Pages { get; } = new();

    /// <summary>
    /// The current page's dot; setting it turns to that page.
    /// </summary>
    public HistoryPage? SelectedPage
    {
        get => Pages.FirstOrDefault(p => p.Number == Page);
        set
        {
            if (value is not null)
                Page = value.Number;
        }
    }

    /// <inheritdoc/>
    public override Task ActivateAsync()
    {
        Reload();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Rereads the history and shows the page the user was on, or the last one that still exists.
    /// </summary>
    public void Reload()
    {
        _entries = _history.All().Select(r => new HistoryEntryViewModel(r)).ToArray();
        Pages.Clear();
        for (var n = 1; n <= PageCount; n++)
            Pages.Add(new HistoryPage(n, $"Page {n}"));
        Page = Math.Min(Math.Max(1, Page), PageCount);
        Show();
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(PageCount));
        OnPropertyChanged(nameof(PageLabel));
        OnPropertyChanged(nameof(SelectedPage));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanGoPrevious));
        NextPageCommand.NotifyCanExecuteChanged();
        PreviousPageCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Forgets a title after asking.
    /// </summary>
    /// <param name="entry">Tile to remove.</param>
    [RelayCommand]
    private async Task ForgetAsync(HistoryEntryViewModel? entry)
    {
        if (entry is null)
            return;
        if (!await _dialogs.ConfirmAsync(DialogTitle, $"Forget {entry.Name}?\n\nThe title that was written stays where it is; only the history entry goes.").ConfigureAwait(true))
            return;

        _history.Remove(entry.Record.Id);
        Reload();
    }

    /// <summary>
    /// Loads a tile into the wizard and shows it, warning first when the ROM has moved.
    /// </summary>
    /// <param name="entry">Tile picked.</param>
    [RelayCommand]
    private async Task LoadAsync(HistoryEntryViewModel? entry)
    {
        if (entry is null)
            return;

        _inject.Load(entry.Record);
        _navigation.Show<InjectViewModel>();
        if (entry.IsRomMissing)
            await _dialogs.ShowInfoAsync(DialogTitle, $"The ROM is no longer at\n{entry.Record.RomPath}\n\nPick it again on the Game step before injecting.").ConfigureAwait(true);
    }

    /// <summary>
    /// Turns to the next page.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void NextPage() => Page++;

    partial void OnPageChanged(int value) => Show();

    /// <summary>
    /// Turns to the previous page.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private void PreviousPage() => Page--;

    /// <summary>
    /// Fills <see cref="Current"/> with the page's tiles.
    /// </summary>
    private void Show()
    {
        Current.Clear();
        foreach (var entry in _entries.Skip((Page - 1) * PageSize).Take(PageSize))
            Current.Add(entry);
    }
}
