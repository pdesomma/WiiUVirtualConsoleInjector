namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// Runs work on the UI thread, now or after a delay.
/// </summary>
public interface IUiScheduler
{
    /// <summary>
    /// Runs the action once the delay has passed; disposing cancels it.
    /// </summary>
    /// <param name="delay">How long to wait.</param>
    /// <param name="action">What to run.</param>
    IDisposable Delay(TimeSpan delay, Action action);

    /// <summary>
    /// Queues the action on the UI thread.
    /// </summary>
    /// <param name="action">What to run.</param>
    void Post(Action action);
}
