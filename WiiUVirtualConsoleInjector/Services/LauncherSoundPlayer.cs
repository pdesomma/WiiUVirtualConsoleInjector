namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// Hands the file to the system's own player where in-process playback is not available.
/// </summary>
public sealed class LauncherSoundPlayer : ISoundPlayer
{
    private readonly ILinkOpener _links;

    /// <summary>
    /// Creates a new instance of the <see cref="LauncherSoundPlayer"/> class.
    /// </summary>
    /// <param name="links">Opens the file with whatever handles it.</param>
    public LauncherSoundPlayer(ILinkOpener links)
    {
        _links = links ?? throw new ArgumentNullException(nameof(links));
    }

    /// <inheritdoc/>
    public event EventHandler? Stopped;

    /// <inheritdoc/>
    public bool IsPlaying => false;

    /// <inheritdoc/>
    public void Play(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));
        if (!File.Exists(path))
            throw new FileNotFoundException("The sound file is not there.", path);

        _ = _links.OpenAsync(new Uri(Path.GetFullPath(path)));
        Stopped?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public void Stop()
    {
    }
}
