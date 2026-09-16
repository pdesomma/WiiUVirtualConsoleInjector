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
    /// Extension of a sound already in the console's format, copied through untouched.
    /// </summary>
    public const string ReadyExtension = ".btsnd";

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
        if (sourcePath is null)
            throw new ArgumentNullException(nameof(sourcePath));
        if (destinationPath is null)
            throw new ArgumentNullException(nameof(destinationPath));

        cancellationToken.ThrowIfCancellationRequested();
        if (string.Equals(Path.GetExtension(sourcePath), ReadyExtension, StringComparison.OrdinalIgnoreCase))
            File.Copy(sourcePath, destinationPath, overwrite: true);
        else
            BootSoundConverter.Convert(sourcePath, destinationPath, Target);
        return Task.CompletedTask;
    }
}
