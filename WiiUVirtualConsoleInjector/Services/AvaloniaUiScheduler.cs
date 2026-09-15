using Avalonia.Threading;

namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// Schedules on Avalonia's UI dispatcher.
/// </summary>
public sealed class AvaloniaUiScheduler : IUiScheduler
{
    /// <inheritdoc/>
    public IDisposable Delay(TimeSpan delay, Action action)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        return DispatcherTimer.RunOnce(action, delay);
    }

    /// <inheritdoc/>
    public void Post(Action action)
    {
        if (action is null)
            throw new ArgumentNullException(nameof(action));

        if (Dispatcher.UIThread.CheckAccess())
            action();
        else
            Dispatcher.UIThread.Post(action);
    }
}
