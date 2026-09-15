using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// One notice in the corner of the window.
/// </summary>
public sealed partial class ToastViewModel : ViewModelBase
{
    private readonly IToastService _toasts;

    [ObservableProperty]
    private bool _isEntering = true;
    [ObservableProperty]
    private bool _isLeaving;

    /// <summary>
    /// Creates a new instance of the <see cref="ToastViewModel"/> class.
    /// </summary>
    /// <param name="kind">What it reports.</param>
    /// <param name="title">Bold first line.</param>
    /// <param name="message">Second line, or null.</param>
    /// <param name="toasts">Where dismissal goes.</param>
    public ToastViewModel(ToastKind kind, string title, string? message, IToastService toasts)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        _toasts = toasts ?? throw new ArgumentNullException(nameof(toasts));
        Kind = kind;
        Message = string.IsNullOrWhiteSpace(message) ? null : message;
    }

    /// <summary>
    /// True when there is a second line.
    /// </summary>
    public bool HasMessage => Message is not null;
    /// <summary>
    /// What it reports.
    /// </summary>
    public ToastKind Kind { get; }
    /// <summary>
    /// Second line, or null.
    /// </summary>
    public string? Message { get; }
    /// <summary>
    /// Bold first line.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Closes this toast.
    /// </summary>
    [RelayCommand]
    private void Dismiss() => _toasts.Dismiss(this);
}
