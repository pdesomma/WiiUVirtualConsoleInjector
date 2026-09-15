using System.Collections.ObjectModel;
using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// Brief notices stacked in the corner of the window.
/// </summary>
public interface IToastService
{
    /// <summary>
    /// What is on screen, oldest first.
    /// </summary>
    ReadOnlyObservableCollection<ToastViewModel> Toasts { get; }

    /// <summary>
    /// Starts one toast leaving; it is gone once the exit animation ends.
    /// </summary>
    /// <param name="toast">Toast to close.</param>
    void Dismiss(ToastViewModel toast);

    /// <summary>
    /// Shows a toast; everything but an error self-dismisses.
    /// </summary>
    /// <param name="kind">What it reports.</param>
    /// <param name="title">Bold first line.</param>
    /// <param name="message">Second line, or null.</param>
    void Show(ToastKind kind, string title, string? message = null);
}
