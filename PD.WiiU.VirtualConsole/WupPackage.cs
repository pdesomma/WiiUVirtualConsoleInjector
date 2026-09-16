using WiiUSharp;
using WiiUSharp.Nus;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// An installable title folder as the console's installers take it: title.tmd, title.tik, title.cert and one encrypted .app per content, with an .h3 beside each hashed one. A game backup from a disc arrives in this shape, as does every packed inject.
/// </summary>
public sealed class WupPackage
{
    /// <summary>
    /// Certificate chain file.
    /// </summary>
    public const string CertificateFile = "title.cert";
    /// <summary>
    /// Ticket file.
    /// </summary>
    public const string TicketFile = "title.tik";
    /// <summary>
    /// Title metadata file.
    /// </summary>
    public const string TmdFile = "title.tmd";

    private WupPackage(string folder, TmdInfo tmd, IReadOnlyList<string> missing)
    {
        Folder = folder;
        Name = Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        TitleId = tmd.Title.TitleId;
        TitleVersion = tmd.Title.TitleVersion;
        ContentCount = tmd.Contents.Count;
        Size = tmd.Contents.Sum(c => c.Size);
        Missing = missing;
    }

    /// <summary>
    /// How many contents the TMD lists.
    /// </summary>
    public int ContentCount { get; }
    /// <summary>
    /// The folder inspected.
    /// </summary>
    public string Folder { get; }
    /// <summary>
    /// True when every file the TMD calls for is there at its size.
    /// </summary>
    public bool IsComplete => Missing.Count == 0;
    /// <summary>
    /// Files that are absent or the wrong size, as "name" or "name (wrong size)".
    /// </summary>
    public IReadOnlyList<string> Missing { get; }
    /// <summary>
    /// The folder's own name; installers show it.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// Bytes of content the TMD lists.
    /// </summary>
    public long Size { get; }
    /// <summary>
    /// Title the package installs.
    /// </summary>
    public TitleId TitleId { get; }
    /// <summary>
    /// Version from the TMD.
    /// </summary>
    public ushort TitleVersion { get; }

    /// <summary>
    /// Reads the TMD and checks every file it calls for.
    /// </summary>
    /// <param name="folder">Package folder.</param>
    /// <exception cref="DirectoryNotFoundException">No such folder.</exception>
    /// <exception cref="InvalidDataException">No title.tmd, or one that does not parse.</exception>
    public static WupPackage Inspect(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
            throw new ArgumentException("Folder is required.", nameof(folder));
        if (!Directory.Exists(folder))
            throw new DirectoryNotFoundException($"{folder} is not there.");
        var tmdPath = Path.Combine(folder, TmdFile);
        if (!File.Exists(tmdPath))
            throw new InvalidDataException($"No {TmdFile} in {folder}.");

        TmdInfo tmd;
        try
        {
            tmd = Tmd.Parse(File.ReadAllBytes(tmdPath));
        }
        catch (Exception e) when (e is ArgumentException or IndexOutOfRangeException or InvalidDataException)
        {
            throw new InvalidDataException($"{TmdFile} in {folder} does not parse: {e.Message}", e);
        }

        var missing = new List<string>();
        foreach (var name in new[] { TicketFile, CertificateFile })
            if (!File.Exists(Path.Combine(folder, name)))
                missing.Add(name);
        foreach (var content in tmd.Contents)
        {
            var app = content.Id.ToString("x8") + ".app";
            var info = new FileInfo(Path.Combine(folder, app));
            if (!info.Exists)
                missing.Add(app);
            else if (info.Length != content.Size)
                missing.Add(app + " (wrong size)");
            if (content.IsHashed && !File.Exists(Path.Combine(folder, content.Id.ToString("x8") + ".h3")))
                missing.Add(content.Id.ToString("x8") + ".h3");
        }
        return new WupPackage(folder, tmd, missing);
    }

    /// <summary>
    /// True when a folder holds a title.tmd.
    /// </summary>
    /// <param name="folder">Folder to look at.</param>
    public static bool LooksLike(string folder) => !string.IsNullOrWhiteSpace(folder) && File.Exists(Path.Combine(folder, TmdFile));

    /// <inheritdoc/>
    public override string ToString() => $"{Name} ({TitleId}, v{TitleVersion})";
}
