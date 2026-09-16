namespace PD.WiiU.VirtualConsole;

/// <summary>
/// A published version of the application and where to get it.
/// </summary>
/// <param name="Version">Version number.</param>
/// <param name="Page">Release page.</param>
public sealed record AppRelease(Version Version, Uri Page)
{
    /// <summary>
    /// True when this is newer than a running version.
    /// </summary>
    /// <param name="current">The running version.</param>
    public bool IsNewerThan(Version current) => Version > (current ?? throw new ArgumentNullException(nameof(current)));
}
