using System.Globalization;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// A size on disk, shown as the console shows it: whole KB under a megabyte, tenths of MB under a gigabyte, hundredths of GB above.
/// </summary>
/// <param name="Bytes">The count.</param>
public readonly record struct ByteSize(long Bytes) : IComparable<ByteSize>
{
    private const long Kilobyte = 1024;
    private const long Megabyte = Kilobyte * 1024;
    private const long Gigabyte = Megabyte * 1024;

    /// <summary>
    /// The text, such as 384 KB, 1.2 MB or 4.41 GB.
    /// </summary>
    public string Text => Unit switch
    {
        ByteSizeUnit.Kilobytes => (Bytes + Kilobyte / 2) / Kilobyte + " KB",
        ByteSizeUnit.Megabytes => ((double)Bytes / Megabyte).ToString("0.#", CultureInfo.InvariantCulture) + " MB",
        _ => ((double)Bytes / Gigabyte).ToString("0.##", CultureInfo.InvariantCulture) + " GB",
    };
    /// <summary>
    /// Which unit the text uses.
    /// </summary>
    public ByteSizeUnit Unit => Bytes < Megabyte ? ByteSizeUnit.Kilobytes : Bytes < Gigabyte ? ByteSizeUnit.Megabytes : ByteSizeUnit.Gigabytes;

    /// <summary>
    /// Every file under a folder; zero when it is not there.
    /// </summary>
    /// <param name="path">Folder.</param>
    public static ByteSize OfDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return default;
        long total = 0;
        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            total += new FileInfo(file).Length;
        return new ByteSize(total);
    }

    /// <summary>
    /// One file; zero when it is not there.
    /// </summary>
    /// <param name="path">File.</param>
    public static ByteSize OfFile(string path) => !string.IsNullOrWhiteSpace(path) && File.Exists(path) ? new ByteSize(new FileInfo(path).Length) : default;

    /// <inheritdoc/>
    public int CompareTo(ByteSize other) => Bytes.CompareTo(other.Bytes);

    /// <inheritdoc/>
    public override string ToString() => Text;
}
