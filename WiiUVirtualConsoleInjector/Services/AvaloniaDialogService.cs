using Avalonia.Controls;
using Avalonia.Platform.Storage;
using WiiUVirtualConsoleInjector.Views;

namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// <see cref="IDialogService"/> over the owning window's storage provider and a small message window.
/// </summary>
public sealed class AvaloniaDialogService : IDialogService
{
    private readonly Func<Window> _owner;

    /// <summary>
    /// Creates a new instance of the <see cref="AvaloniaDialogService"/> class.
    /// </summary>
    /// <param name="owner">Resolves the window dialogs are modal to.</param>
    public AvaloniaDialogService(Func<Window> owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    /// <inheritdoc/>
    public Task<bool> ConfirmAsync(string title, string message) =>
        MessageWindow.ShowAsync(_owner(), title, message, MessageWindow.Kind.Question);

    /// <inheritdoc/>
    public async Task<string?> PickFolderAsync(string title, string? startFolder = null)
    {
        var provider = _owner().StorageProvider;
        var options = new FolderPickerOpenOptions { Title = title, AllowMultiple = false };
        if (startFolder is not null && Directory.Exists(startFolder))
            options.SuggestedStartLocation = await provider.TryGetFolderFromPathAsync(startFolder).ConfigureAwait(true);

        var folders = await provider.OpenFolderPickerAsync(options).ConfigureAwait(true);
        return folders.Count == 0 ? null : folders[0].TryGetLocalPath();
    }

    /// <inheritdoc/>
    public async Task<string?> PickOpenFileAsync(string title, params FileFilter[] filters)
    {
        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = filters.Select(f => new FilePickerFileType(f.Name) { Patterns = f.Patterns }).ToArray(),
        };
        var files = await _owner().StorageProvider.OpenFilePickerAsync(options).ConfigureAwait(true);
        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }

    /// <inheritdoc/>
    public Task ShowErrorAsync(string title, string message) =>
        MessageWindow.ShowAsync(_owner(), title, message, MessageWindow.Kind.Error);

    /// <inheritdoc/>
    public Task ShowInfoAsync(string title, string message) =>
        MessageWindow.ShowAsync(_owner(), title, message, MessageWindow.Kind.Information);

    /// <inheritdoc/>
    public Task ShowImageAsync(string title, string path) => ImageWindow.ShowAsync(_owner(), title, path);
}
