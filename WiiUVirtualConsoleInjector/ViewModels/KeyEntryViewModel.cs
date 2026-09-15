using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// One key the user pastes in: masked text that stores itself once valid, save, clear and whether one is stored.
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

    private const int KeySize = 16;

    private readonly Action _changed;
    private readonly Action _clear;
    private readonly IDialogService _dialogs;
    private readonly Action<string> _save;
    private readonly Func<string?> _stored;

    [ObservableProperty]
    private bool _isRevealed;
    [ObservableProperty]
    private string _status = NotSetText;
    private bool _syncing;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
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
    /// True when the text is 32 hex characters.
    /// </summary>
    public bool IsValid => HexKeyText.IsValid(Text, KeySize);
    /// <summary>
    /// Name shown to the user.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Re-reads the stored key into the text box and status.
    /// </summary>
    public void Refresh()
    {
        _syncing = true;
        try
        {
            var stored = _stored();
            Text = stored ?? string.Empty;
            Status = stored is null ? NotSetText : SetText;
        }
        finally
        {
            _syncing = false;
        }
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

    partial void OnStatusChanged(string value) => OnPropertyChanged(nameof(IsSet));

    /// <summary>
    /// Stores as soon as the hex is complete; emptying forgets the key.
    /// </summary>
    /// <param name="value">What the user typed.</param>
    partial void OnTextChanged(string value)
    {
        if (_syncing)
            return;

        if (string.IsNullOrWhiteSpace(value))
        {
            if (IsSet)
                Clear();
        }
        else if (IsValid)
        {
            Store();
        }
    }

    /// <summary>
    /// Parses and stores the text; invalid hex is reported and nothing changes.
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (Store() is { } error)
            await _dialogs.ShowErrorAsync(Label, error.Message);
    }

    /// <summary>
    /// Parses and stores the text; returns the format error, or null once stored.
    /// </summary>
    private FormatException? Store()
    {
        try
        {
            _save(Text);
        }
        catch (FormatException e)
        {
            return e;
        }

        Refresh();
        _changed();
        return null;
    }
}
