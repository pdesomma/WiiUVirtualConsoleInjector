using NAudio.Wave;
using WiiUSharp;
using WiiUSharp.Audio;

namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// Plays a boot sound the way the console will hear it: decoded through the same converter, then out through NAudio.
/// </summary>
public sealed class NAudioSoundPlayer : ISoundPlayer, IDisposable
{
    private readonly IUiScheduler _scheduler;
    private WaveOutEvent? _output;
    private RawSourceWaveStream? _stream;

    /// <summary>
    /// Creates a new instance of the <see cref="NAudioSoundPlayer"/> class.
    /// </summary>
    /// <param name="scheduler">Brings the stop notice back to the UI thread.</param>
    public NAudioSoundPlayer(IUiScheduler scheduler)
    {
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
    }

    /// <inheritdoc/>
    public event EventHandler? Stopped;

    /// <inheritdoc/>
    public bool IsPlaying => _output?.PlaybackState == PlaybackState.Playing;

    /// <inheritdoc/>
    public void Dispose() => Release();

    /// <inheritdoc/>
    public void Play(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));
        if (!File.Exists(path))
            throw new FileNotFoundException("The sound file is not there.", path);

        Release();
        var sound = BootSoundConverter.Load(path);
        var bytes = new byte[sound.FrameCount * BootSound.Channels * sizeof(short)];
        Buffer.BlockCopy(sound.Samples, 0, bytes, 0, bytes.Length);
        _stream = new RawSourceWaveStream(new MemoryStream(bytes), new WaveFormat(BootSound.SampleRate, BootSound.BitsPerSample, BootSound.Channels));
        _output = new WaveOutEvent();
        _output.PlaybackStopped += OnPlaybackStopped;
        _output.Init(_stream);
        _output.Play();
    }

    /// <inheritdoc/>
    public void Stop() => _output?.Stop();

    /// <summary>
    /// Raises <see cref="Stopped"/> on the UI thread once the device has stopped.
    /// </summary>
    /// <param name="sender">Output device.</param>
    /// <param name="e">Why it stopped.</param>
    private void OnPlaybackStopped(object? sender, StoppedEventArgs e) =>
        _scheduler.Post(() => Stopped?.Invoke(this, EventArgs.Empty));

    /// <summary>
    /// Drops the device and stream from the last play.
    /// </summary>
    private void Release()
    {
        if (_output is not null)
        {
            _output.PlaybackStopped -= OnPlaybackStopped;
            _output.Dispose();
            _output = null;
        }

        _stream?.Dispose();
        _stream = null;
    }
}
