using PD.WiiU.VirtualConsole.Ports;

namespace PD.WiiU.VirtualConsole.Infrastructure;

/// <summary>
/// The machine's ready removable volumes, as the runtime reports them.
/// </summary>
public sealed class SystemRemovableDrives : IRemovableDrives
{
    /// <summary>
    /// Folders a card prepared for the Wii U has.
    /// </summary>
    private static readonly string[] Markers = { "wiiu", SdCard.InstallFolder };

    /// <inheritdoc/>
    public IReadOnlyList<RemovableDrive> List() => DriveInfo.GetDrives()
        .Where(Usable)
        .Select(Describe)
        .Where(d => d is not null)
        .Select(d => d!)
        .ToArray();

    /// <summary>
    /// One drive's details, or null when it stopped being readable mid-scan.
    /// </summary>
    /// <param name="drive">Drive to describe.</param>
    private static RemovableDrive? Describe(DriveInfo drive)
    {
        try
        {
            return new RemovableDrive(
                drive.RootDirectory.FullName,
                drive.VolumeLabel ?? string.Empty,
                drive.AvailableFreeSpace,
                drive.TotalSize,
                Markers.Any(m => Directory.Exists(Path.Combine(drive.RootDirectory.FullName, m))));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// True for a removable volume that is ready to read.
    /// </summary>
    /// <param name="drive">Drive to test.</param>
    private static bool Usable(DriveInfo drive)
    {
        try
        {
            return drive.DriveType == DriveType.Removable && drive.IsReady;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
