using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp;
using WiiUSharp.Audio;

namespace PD.WiiU.VirtualConsole.Infrastructure;

/// <summary>
/// Boot sound conversion via WiiUSharp.Audio.
/// </summary>
public sealed class NAudioBootSoundConverter : IBootSoundConverter
{
    /// <summary>
    /// Creates a new instance of the <see cref="NAudioBootSoundConverter"/> class.
    /// </summary>
    /// <param name="target">Where the sound plays.</param>
    public NAudioBootSoundConverter(BootSoundTarget target = BootSoundTarget.Both)
    {
        Target = target;
    }

    /// <summary>
    /// Where the sound plays.
    /// </summary>
    public BootSoundTarget Target { get; }

    /// <inheritdoc/>
    public Task ConvertAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BootSoundConverter.Convert(sourcePath, destinationPath, Target);
        return Task.CompletedTask;
    }
}
