using CommunityToolkit.Mvvm.ComponentModel;
using PD.WiiU.VirtualConsole;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// One BIOS file a core needs on the card: whether it is already there, and the copy the user offers when it is not.
/// </summary>
public sealed partial class BiosFileViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWanted), nameof(Status))]
    private bool _isOnCard;

    /// <summary>
    /// Creates a new instance of the <see cref="BiosFileViewModel"/> class.
    /// </summary>
    /// <param name="dialogs">Opens the picker.</param>
    /// <param name="fileName">Name the core looks for.</param>
    public BiosFileViewModel(IDialogService dialogs, string fileName)
    {
        if (dialogs is null)
            throw new ArgumentNullException(nameof(dialogs));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));

        FileName = fileName;
        CardPath = RetroArchSystem.SystemFolder + "/" + fileName;
        Field = new PathFieldViewModel(dialogs, fileName, "Copied to SD:/" + CardPath + " with the title", new FileFilter(fileName, fileName), new FileFilter("All files", "*.*"));
        Field.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PathFieldViewModel.Path))
            {
                OnPropertyChanged(nameof(IsWanted));
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(CardFile));
            }
        };
    }

    /// <summary>
    /// The copy to make, or null when none is offered or none is needed.
    /// </summary>
    public CardFile? CardFile => !IsOnCard && Field.HasPath ? new CardFile(Field.Path!, CardPath) : null;
    /// <summary>
    /// Where the file lives on the card, relative to its root.
    /// </summary>
    public string CardPath { get; }
    /// <summary>
    /// The user's copy of the file.
    /// </summary>
    public PathFieldViewModel Field { get; }
    /// <summary>
    /// Name the core looks for.
    /// </summary>
    public string FileName { get; }
    /// <summary>
    /// True while the card lacks the file and nothing has been picked to copy.
    /// </summary>
    public bool IsWanted => !IsOnCard && !Field.HasPath;
    /// <summary>
    /// One line on where things stand.
    /// </summary>
    public string Status => IsOnCard
        ? "Already on the SD card."
        : Field.HasPath
            ? $"Will be copied to SD:/{CardPath} ({ByteSize.OfFile(Field.Path!)})."
            : "Not on the SD card. Pick your own dump to copy it along; the core will not start without it.";
}
