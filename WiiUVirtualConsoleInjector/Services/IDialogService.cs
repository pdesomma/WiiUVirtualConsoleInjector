namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// The pickers and message boxes view models need without touching Avalonia.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Yes/no question; true for yes.
    /// </summary>
    /// <param name="title">Window title.</param>
    /// <param name="message">Question text.</param>
    Task<bool> ConfirmAsync(string title, string message);

    /// <summary>
    /// A folder picked by the user, or null when cancelled.
    /// </summary>
    /// <param name="title">Picker title.</param>
    /// <param name="startFolder">Folder to open at, when it exists.</param>
    Task<string?> PickFolderAsync(string title, string? startFolder = null);

    /// <summary>
    /// A file picked by the user, or null when cancelled.
    /// </summary>
    /// <param name="title">Picker title.</param>
    /// <param name="filters">File types offered.</param>
    Task<string?> PickOpenFileAsync(string title, params FileFilter[] filters);

    /// <summary>
    /// Shows an error and waits for it to be dismissed.
    /// </summary>
    /// <param name="title">Window title.</param>
    /// <param name="message">What went wrong.</param>
    Task ShowErrorAsync(string title, string message);

    /// <summary>
    /// Shows a notice and waits for it to be dismissed.
    /// </summary>
    /// <param name="title">Window title.</param>
    /// <param name="message">Notice text.</param>
    Task ShowInfoAsync(string title, string message);

    /// <summary>
    /// Shows an image at its own size, as large as the screen allows; a click or Escape closes it.
    /// </summary>
    /// <param name="title">Window title.</param>
    /// <param name="path">Image file.</param>
    Task ShowImageAsync(string title, string path);
}
