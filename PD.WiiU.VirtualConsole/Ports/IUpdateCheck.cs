namespace PD.WiiU.VirtualConsole.Ports;

/// <summary>
/// Where the newest published version of the application is found.
/// </summary>
public interface IUpdateCheck
{
    /// <summary>
    /// The newest release, or null when nothing has been published.
    /// </summary>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    Task<AppRelease?> LatestAsync(CancellationToken cancellationToken = default);
}
