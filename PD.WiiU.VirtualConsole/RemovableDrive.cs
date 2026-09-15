namespace PD.WiiU.VirtualConsole;

/// <summary>
/// One removable volume the SD card could be.
/// </summary>
/// <param name="RootPath">Root of the volume, such as E:\.</param>
/// <param name="Label">Volume label, blank when it has none.</param>
/// <param name="FreeBytes">Space left on it.</param>
/// <param name="TotalBytes">Size of it.</param>
/// <param name="LooksPrepared">True when it already holds the folders a Wii U card has.</param>
public sealed record RemovableDrive(string RootPath, string Label, long FreeBytes, long TotalBytes, bool LooksPrepared)
{
    /// <summary>
    /// Root, label and free space, as the drive list shows it.
    /// </summary>
    public string Description => $"{RootPath}{(string.IsNullOrWhiteSpace(Label) ? string.Empty : "  " + Label)}  ({Gigabytes(FreeBytes)} free of {Gigabytes(TotalBytes)})";

    /// <inheritdoc/>
    public override string ToString() => Description;

    /// <summary>
    /// A byte count in whole tenths of a gigabyte.
    /// </summary>
    /// <param name="bytes">Count to format.</param>
    private static string Gigabytes(long bytes) => (bytes / 1073741824d).ToString("0.#", System.Globalization.CultureInfo.CurrentCulture) + " GB";
}
