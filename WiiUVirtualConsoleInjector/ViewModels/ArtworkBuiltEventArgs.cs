namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// The three images a build produced.
/// </summary>
public sealed class ArtworkBuiltEventArgs : EventArgs
{
    /// <summary>
    /// Creates a new instance of the <see cref="ArtworkBuiltEventArgs"/> class.
    /// </summary>
    /// <param name="iconPath">Menu icon PNG.</param>
    /// <param name="bootTvPath">TV boot screen PNG.</param>
    /// <param name="bootDrcPath">GamePad boot screen PNG.</param>
    public ArtworkBuiltEventArgs(string iconPath, string bootTvPath, string bootDrcPath)
    {
        IconPath = iconPath ?? throw new ArgumentNullException(nameof(iconPath));
        BootTvPath = bootTvPath ?? throw new ArgumentNullException(nameof(bootTvPath));
        BootDrcPath = bootDrcPath ?? throw new ArgumentNullException(nameof(bootDrcPath));
    }

    /// <summary>
    /// GamePad boot screen PNG.
    /// </summary>
    public string BootDrcPath { get; }
    /// <summary>
    /// TV boot screen PNG.
    /// </summary>
    public string BootTvPath { get; }
    /// <summary>
    /// Menu icon PNG.
    /// </summary>
    public string IconPath { get; }
}
