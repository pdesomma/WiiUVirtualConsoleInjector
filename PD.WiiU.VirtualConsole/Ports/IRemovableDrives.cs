namespace PD.WiiU.VirtualConsole.Ports;

/// <summary>
/// The removable volumes the machine has right now.
/// </summary>
public interface IRemovableDrives
{
    /// <summary>
    /// Every ready removable volume, in drive order.
    /// </summary>
    IReadOnlyList<RemovableDrive> List();
}
