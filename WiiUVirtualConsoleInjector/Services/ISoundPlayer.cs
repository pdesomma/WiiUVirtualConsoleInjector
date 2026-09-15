namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// Plays one audio file at a time so the user can hear a boot sound before injecting it.
/// </summary>
public interface ISoundPlayer
{
    /// <summary>
    /// Raised on the UI thread when playback ends or is stopped.
    /// </summary>
    event EventHandler? Stopped;

    /// <summary>
    /// True while something is playing.
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Starts the file, stopping whatever was playing.
    /// </summary>
    /// <param name="path">Audio file.</param>
    /// <exception cref="FileNotFoundException">The file is not there.</exception>
    /// <exception cref="InvalidDataException">The file cannot be decoded.</exception>
    void Play(string path);

    /// <summary>
    /// Stops playback; nothing happens when idle.
    /// </summary>
    void Stop();
}
