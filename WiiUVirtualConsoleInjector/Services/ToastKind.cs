namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// What a toast reports, which picks its stripe colour and whether it self-dismisses.
/// </summary>
public enum ToastKind
{
    /// <summary>
    /// Something finished.
    /// </summary>
    Success,
    /// <summary>
    /// Something failed; stays until closed.
    /// </summary>
    Error,
    /// <summary>
    /// Something needs attention.
    /// </summary>
    Warning,
    /// <summary>
    /// Plain notice.
    /// </summary>
    Info,
}
