using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Options;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels.Options;

/// <summary>
/// Editable <see cref="ArcadeOptions"/>: the zips a romset needs beside the game.
/// </summary>
public sealed partial class ArcadeOptionsViewModel : ConsoleOptionsViewModel
{
    private const string ArcadeHint = "Clones need their parent set beside them (sf2ce needs sf2.zip); some sets need a BIOS zip (qsound_hle.zip) or samples. Add them here; each is copied into the title next to the game, name unchanged.";
    private const string NeoGeoHint = "Neo Geo games need neogeo.zip beside them. Add it here; it is copied into the title next to the game.";

    private static readonly FileFilter[] Filters = { new("Romset zips", "*.zip", "*.7z") };

    private readonly IDialogService _dialogs;

    /// <summary>
    /// Creates a new instance of the <see cref="ArcadeOptionsViewModel"/> class.
    /// </summary>
    /// <param name="dialogs">Picker for the companion zips.</param>
    /// <param name="console">Arcade or Neo Geo.</param>
    /// <exception cref="ArgumentOutOfRangeException">Not an arcade console.</exception>
    public ArcadeOptionsViewModel(IDialogService dialogs, SourceConsole console)
        : base(console)
    {
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        Hint = console switch
        {
            SourceConsole.Arcade => ArcadeHint,
            SourceConsole.NeoGeo => NeoGeoHint,
            _ => throw new ArgumentOutOfRangeException(nameof(console), console, "Only Arcade and Neo Geo take companion zips."),
        };
        Companions.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasCompanions));
    }

    /// <summary>
    /// Full paths of the zips copied beside the game.
    /// </summary>
    public ObservableCollection<string> Companions { get; } = new();

    /// <summary>
    /// True once a zip is listed.
    /// </summary>
    public bool HasCompanions => Companions.Count > 0;

    /// <summary>
    /// Why the console needs companions and what happens to them.
    /// </summary>
    public string Hint { get; }

    /// <inheritdoc/>
    public override IConsoleOptions? Build() => new ArcadeOptions
    {
        Console = Console,
        CompanionPaths = Companions.ToArray(),
    };

    /// <inheritdoc/>
    public override void Load(IConsoleOptions? options)
    {
        Companions.Clear();
        foreach (var path in (options as ArcadeOptions)?.CompanionPaths ?? Array.Empty<string>())
            Companions.Add(path);
    }

    /// <summary>
    /// Picks a zip and adds it unless the same path is already listed.
    /// </summary>
    [RelayCommand]
    private async Task AddCompanionAsync()
    {
        var picked = await _dialogs.PickOpenFileAsync("Companion zip", Filters).ConfigureAwait(true);
        if (picked is null || Companions.Contains(picked, StringComparer.OrdinalIgnoreCase))
            return;

        Companions.Add(picked);
    }

    /// <summary>
    /// Drops a listed zip.
    /// </summary>
    /// <param name="path">Path as listed.</param>
    [RelayCommand]
    private void RemoveCompanion(string? path)
    {
        if (path is not null)
            Companions.Remove(path);
    }
}
