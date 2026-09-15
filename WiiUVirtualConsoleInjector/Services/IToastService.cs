namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// Brief, self-dismissing notices in the corner of the window.
/// </summary>
public interface IToastService
{
    /// <summary>
    /// Shows a notice for a moment.
    /// </summary>
    /// <param name="message">Short text.</param>
    void Show(string message);
}
