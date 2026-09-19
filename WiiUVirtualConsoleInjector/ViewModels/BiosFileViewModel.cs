using CommunityToolkit.Mvvm.ComponentModel;
using PD.WiiU.VirtualConsole;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// One BIOS a core needs on the card: whether it is already there, the copy the user offers when it is not, and the accepted name it is saved under.
/// </summary>
public sealed partial class BiosFileViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOnCard), nameof(IsWanted), nameof(Status), nameof(CardFile))]
    private string? _onCardAs;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CardPath), nameof(CardFile), nameof(Status))]
    private string _selectedName;

    /// <summary>
    /// Creates a new instance of the <see cref="BiosFileViewModel"/> class.
    /// </summary>
    /// <param name="dialogs">Opens the picker.</param>
    /// <param name="bios">The BIOS and the names the core accepts for it.</param>
    public BiosFileViewModel(IDialogService dialogs, BiosFile bios)
    {
        if (dialogs is null)
            throw new ArgumentNullException(nameof(dialogs));
        Bios = bios ?? throw new ArgumentNullException(nameof(bios));

        _selectedName = bios.Names[0];
        Field = new PathFieldViewModel(dialogs, bios.Label, "Copied to SD:/" + RetroArchSystem.SystemFolder + " with the title", new FileFilter(bios.Label, bios.Names.ToArray()), new FileFilter("All files", "*.*"));
        Field.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(PathFieldViewModel.Path))
                return;
            if (Field.Path is { } path)
                SelectedName = bios.NameFor(path);
            OnPropertyChanged(nameof(IsWanted));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(CardFile));
        };
    }

    /// <summary>
    /// The BIOS and the names the core accepts for it.
    /// </summary>
    public BiosFile Bios { get; }
    /// <summary>
    /// The copy to make, or null when none is offered or none is needed.
    /// </summary>
    public CardFile? CardFile => !IsOnCard && Field.HasPath ? new CardFile(Field.Path!, CardPath) : null;
    /// <summary>
    /// Where the copy lands on the card, relative to its root, under the selected name.
    /// </summary>
    public string CardPath => RetroArchSystem.SystemFolder + "/" + SelectedName;
    /// <summary>
    /// The user's copy of the file.
    /// </summary>
    public PathFieldViewModel Field { get; }
    /// <summary>
    /// True when the core accepts more than one name, so the user can choose.
    /// </summary>
    public bool HasChoice => Names.Count > 1;
    /// <summary>
    /// True when the card already has the file under one of the accepted names.
    /// </summary>
    public bool IsOnCard => OnCardAs is not null;
    /// <summary>
    /// True while the card lacks the file and nothing has been picked to copy.
    /// </summary>
    public bool IsWanted => !IsOnCard && !Field.HasPath;
    /// <summary>
    /// What the file is, e.g. "PlayStation BIOS".
    /// </summary>
    public string Label => Bios.Label;
    /// <summary>
    /// File names the core accepts, preferred first.
    /// </summary>
    public IReadOnlyList<string> Names => Bios.Names;
    /// <summary>
    /// One line on where things stand.
    /// </summary>
    public string Status => IsOnCard
        ? $"Already on the SD card ({OnCardAs})."
        : Field.HasPath
            ? $"Will be copied to SD:/{CardPath} ({ByteSize.OfFile(Field.Path!)})."
            : "Not on the SD card. Pick your own dump to copy it along; the core will not start without it.";
}
