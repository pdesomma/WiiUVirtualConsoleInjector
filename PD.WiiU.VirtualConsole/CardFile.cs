namespace PD.WiiU.VirtualConsole;

/// <summary>
/// A file that goes onto the SD card beside the title, such as a BIOS a core needs.
/// </summary>
public sealed class CardFile
{
    /// <summary>
    /// Creates a new instance of the <see cref="CardFile"/> class.
    /// </summary>
    /// <param name="sourcePath">File on this machine.</param>
    /// <param name="cardPath">Where it lands, relative to the card root, with forward slashes, e.g. retroarch/system/lynxboot.img.</param>
    public CardFile(string sourcePath, string cardPath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("Source path is required.", nameof(sourcePath));
        if (string.IsNullOrWhiteSpace(cardPath))
            throw new ArgumentException("Card path is required.", nameof(cardPath));
        var normalized = cardPath.Replace('\\', '/').Trim('/');
        if (normalized.Length == 0 || normalized.Split('/').Any(part => part.Length == 0 || part == "." || part == ".."))
            throw new ArgumentException("Card path must be a plain relative path.", nameof(cardPath));

        SourcePath = sourcePath;
        CardPath = normalized;
    }

    /// <summary>
    /// Where it lands, relative to the card root, with forward slashes.
    /// </summary>
    public string CardPath { get; }
    /// <summary>
    /// File name on the card.
    /// </summary>
    public string FileName => CardPath.Substring(CardPath.LastIndexOf('/') + 1);
    /// <summary>
    /// Size of the source, or zero when it is not there.
    /// </summary>
    public ByteSize Size => ByteSize.OfFile(SourcePath);
    /// <summary>
    /// File on this machine.
    /// </summary>
    public string SourcePath { get; }

    /// <summary>
    /// Full path of the file on a card.
    /// </summary>
    /// <param name="root">Card root.</param>
    public string On(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Card root is required.", nameof(root));
        return Path.Combine(root, CardPath.Replace('/', Path.DirectorySeparatorChar));
    }

    /// <inheritdoc/>
    public override string ToString() => $"{SourcePath} -> {CardPath}";
}
