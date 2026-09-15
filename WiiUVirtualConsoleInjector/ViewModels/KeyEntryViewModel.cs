using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// One key the user pastes in: masked text, save, clear and whether one is stored.
/// </summary>
public sealed partial class KeyEntryViewModel : ViewModelBase
{
    /// <summary>
    /// Status text when no key is stored.
    /// </summary>
    public const string NotSetText = "Not set";
    /// <summary>
    /// Status text when a key is stored.
    /// </summary>
    public const string SetText = "Set";

    private readonly Action _changed;
    private readonly Action _clear;
    private readonly IDialogService _dialogs;
    private readonly Action<string> _save;
    private readonly Func<string?> _stored;

    [ObservableProperty]
    private bool _isRevealed;
    [ObservableProperty]
    private string _status = NotSetText;
    [ObservableProperty]
    private string _text = string.Empty;

    /// <summary>
    /// Creates a new instance of the <see cref="KeyEntryViewModel"/> class.
    /// </summary>
    /// <param name="label">Name shown to the user.</param>
    /// <param name="stored">The stored key as hex, or null.</param>
    /// <param name="save">Parses hex and stores it; throws <see cref="FormatException"/> when invalid.</param>
    /// <param name="clear">Forgets the stored key.</param>
    /// <param name="dialogs">Error messages.</param>
    /// <param name="changed">Runs after the stored key changes.</param>
    public KeyEntryViewModel(string label, Func<string?> stored, Action<string> save, Action clear, IDialogService dialogs, Action changed)
    {
        Label = label ?? throw new ArgumentNullException(nameof(label));
        _stored = stored ?? throw new ArgumentNullException(nameof(stored));
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _clear = clear ?? throw new ArgumentNullException(nameof(clear));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _changed = changed ?? throw new ArgumentNullException(nameof(changed));
        Refresh();
    }

    /// <summary>
    /// True when a key is stored.
    /// </summary>
    public bool IsSet => Status == SetText;
    /// <summary>
    /// Name shown to the user.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Re-reads the stored key into the text box and status.
    /// </summary>
    public void Refresh()
    {
        var stored = _stored();
        Text = stored ?? string.Empty;
        Status = stored is null ? NotSetText : SetText;
    }

    /// <summary>
    /// Forgets the stored key.
    /// </summary>
    [RelayCommand]
    private void Clear()
    {
        _clear();
        Refresh();
        _changed();
    }

    /// <summary>
    /// Parses and stores the text; invalid hex is reported and nothing changes.
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            _save(Text);
        }
        catch (FormatException e)
        {
            await _dialogs.ShowErrorAsync(Label, e.Message);
            return;
        }

        Refresh();
        _changed();
    }

    partial void OnStatusChanged(string value) => OnPropertyChanged(nameof(IsSet));
}
