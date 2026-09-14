namespace PD.WiiU.VirtualConsole;

/// <summary>
/// An injection step failed.
/// </summary>
public sealed class InjectionException : Exception
{
    /// <summary>
    /// Creates a new instance of the <see cref="InjectionException"/> class.
    /// </summary>
    /// <param name="step">Step that failed.</param>
    /// <param name="message">What went wrong.</param>
    /// <param name="innerException">Underlying failure.</param>
    public InjectionException(InjectionStep step, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Step = step;
    }

    /// <summary>
    /// Step that failed.
    /// </summary>
    public InjectionStep Step { get; }
}
