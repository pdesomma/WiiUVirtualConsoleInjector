using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// A labelled path with Pick and Clear: a file matching the filters, or a folder when none are given.
/// </summary>
public sealed partial class PathFieldViewModel : ViewModelBase
{
    private readonly IDialogService _dialogs;
    private readonly FileFilter[] _filters;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPath))]
    [NotifyCanExecuteChangedFor(nameof(ClearCommand))]
    private string? _path;

    /// <summary>
    /// Creates a new instance of the <see cref="PathFieldViewModel"/> class.
    /// </summary>
    /// <param name="dialogs">Picker to open.</param>
    /// <param name="label">Row label.</param>
    /// <param name="hint">Short note shown next to the label, such as a size.</param>
    /// <param name="filters">File types offered; empty picks a folder instead.</param>
    public PathFieldViewModel(IDialogService dialogs, string label, string? hint, params FileFilter[] filters)
    {
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        Label = label ?? throw new ArgumentNullException(nameof(label));
        Hint = hint;
        _filters = filters ?? Array.Empty<FileFilter>();
    }

    /// <summary>
    /// True once a path is set.
    /// </summary>
    public bool HasPath => !string.IsNullOrWhiteSpace(Path);

    /// <summary>
    /// Short note shown next to the label.
    /// </summary>
    public string? Hint { get; }

    /// <summary>
    /// True when the picker chooses a folder.
    /// </summary>
    public bool IsFolder => _filters.Length == 0;

    /// <summary>
    /// Row label.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Forgets the path.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasPath))]
    private void Clear() => Path = null;

    /// <summary>
    /// Opens the picker and keeps the choice, if any.
    /// </summary>
    [RelayCommand]
    private async Task PickAsync()
    {
        var picked = IsFolder
            ? await _dialogs.PickFolderAsync(Label).ConfigureAwait(true)
            : await _dialogs.PickOpenFileAsync(Label, _filters).ConfigureAwait(true);
        if (picked is not null)
            Path = picked;
    }
}
