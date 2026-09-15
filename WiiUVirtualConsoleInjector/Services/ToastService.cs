using System.Collections.ObjectModel;
using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// Keeps the visible toasts: newest last, four at most, each timed out unless it is an error.
/// </summary>
public sealed class ToastService : IToastService
{
    /// <summary>
    /// How long a new toast sits off-screen before it slides in.
    /// </summary>
    public static readonly TimeSpan EnterDelay = TimeSpan.FromMilliseconds(20);
    /// <summary>
    /// How long a toast takes to slide out once dismissed.
    /// </summary>
    public static readonly TimeSpan ExitDuration = TimeSpan.FromMilliseconds(140);
    /// <summary>
    /// How long a toast stays before dismissing itself.
    /// </summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How many toasts are on screen at once; older ones drop off.
    /// </summary>
    public const int MaxVisible = 4;

    private readonly IUiScheduler _scheduler;
    private readonly Dictionary<ToastViewModel, IDisposable> _timers = new();
    private readonly ObservableCollection<ToastViewModel> _toasts = new();

    /// <summary>
    /// Creates a new instance of the <see cref="ToastService"/> class.
    /// </summary>
    /// <param name="scheduler">Marshals to the UI thread and times the dismissals.</param>
    public ToastService(IUiScheduler scheduler)
    {
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        Toasts = new ReadOnlyObservableCollection<ToastViewModel>(_toasts);
    }

    /// <inheritdoc/>
    public ReadOnlyObservableCollection<ToastViewModel> Toasts { get; }

    /// <inheritdoc/>
    public void Dismiss(ToastViewModel toast)
    {
        if (toast is null)
            throw new ArgumentNullException(nameof(toast));
        if (toast.IsLeaving || !_toasts.Contains(toast))
            return;

        Cancel(toast);
        toast.IsLeaving = true;
        _timers[toast] = _scheduler.Delay(ExitDuration, () => Remove(toast));
    }

    /// <inheritdoc/>
    public void Show(ToastKind kind, string title, string? message = null)
    {
        if (title is null)
            throw new ArgumentNullException(nameof(title));

        _scheduler.Post(() =>
        {
            var toast = new ToastViewModel(kind, title, message, this);
            _toasts.Add(toast);
            while (_toasts.Count > MaxVisible)
                Remove(_toasts[0]);

            _scheduler.Delay(EnterDelay, () => toast.IsEntering = false);
            if (kind != ToastKind.Error)
                _timers[toast] = _scheduler.Delay(Lifetime, () => Dismiss(toast));
        });
    }

    /// <summary>
    /// Drops the toast's pending timer, when it has one.
    /// </summary>
    /// <param name="toast">Toast to stop timing.</param>
    private void Cancel(ToastViewModel toast)
    {
        if (_timers.Remove(toast, out var timer))
            timer.Dispose();
    }

    /// <summary>
    /// Takes the toast off screen at once.
    /// </summary>
    /// <param name="toast">Toast to remove.</param>
    private void Remove(ToastViewModel toast)
    {
        Cancel(toast);
        _toasts.Remove(toast);
    }
}
