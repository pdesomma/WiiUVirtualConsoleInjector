using CommunityToolkit.Mvvm.Input;
using PD.WiiU.VirtualConsole;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// One tile on the history grid.
/// </summary>
public sealed class HistoryEntryViewModel : ViewModelBase
{
    /// <summary>
    /// Creates a new instance of the <see cref="HistoryEntryViewModel"/> class.
    /// </summary>
    /// <param name="record">The inject it shows.</param>
    /// <param name="load">What loading the tile does.</param>
    /// <param name="forget">What forgetting the tile does.</param>
    public HistoryEntryViewModel(InjectionRecord record, Func<HistoryEntryViewModel, Task>? load = null, Func<HistoryEntryViewModel, Task>? forget = null)
    {
        Record = record ?? throw new ArgumentNullException(nameof(record));
        LoadCommand = new AsyncRelayCommand(() => load?.Invoke(this) ?? Task.CompletedTask);
        ForgetCommand = new AsyncRelayCommand(() => forget?.Invoke(this) ?? Task.CompletedTask);
    }

    /// <summary>
    /// Console it was built for.
    /// </summary>
    public string ConsoleName => Assets.ConsoleIcons.DisplayName(Record.Console);
    /// <summary>
    /// Console it was built for.
    /// </summary>
    public SourceConsole Console => Record.Console;
    /// <summary>
    /// Forgets this tile; the page decides how.
    /// </summary>
    public IAsyncRelayCommand ForgetCommand { get; }
    /// <summary>
    /// True when the icon PNG is on disk.
    /// </summary>
    public bool HasIcon => Record.IconPath is { } path && File.Exists(path);
    /// <summary>
    /// The shipped icon, or null when it was not captured.
    /// </summary>
    public string? IconPath => HasIcon ? Record.IconPath : null;
    /// <summary>
    /// True when the ROM is no longer where it was.
    /// </summary>
    public bool IsRomMissing => !File.Exists(Record.RomPath);
    /// <summary>
    /// Loads this tile into the wizard; the page decides how.
    /// </summary>
    public IAsyncRelayCommand LoadCommand { get; }
    /// <summary>
    /// What the tile says.
    /// </summary>
    public string Name => Record.DisplayName;
    /// <summary>
    /// The inject it shows.
    /// </summary>
    public InjectionRecord Record { get; }
    /// <summary>
    /// Hover text: console, when, IDs and where the ROM was.
    /// </summary>
    public string Tooltip =>
        $"{string.Join(" / ", Record.Name.Split(',').Select(l => l.Trim()))}\n{ConsoleName} · {Record.CreatedAt.LocalDateTime:g}\n{Record.Identity.ProductCode} · {Record.Identity.TitleId}\n{Record.RomPath}"
        + (IsRomMissing ? "\n⚠ ROM not found" : string.Empty);
    /// <summary>
    /// When it was made.
    /// </summary>
    public string When => Record.CreatedAt.LocalDateTime.ToString("d");
}
