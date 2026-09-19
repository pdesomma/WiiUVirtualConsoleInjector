namespace PD.WiiU.VirtualConsole;

/// <summary>
/// A BIOS a core needs on the card, satisfied by any one of several file names; a name may carry a subfolder when the core keeps its BIOS under one.
/// </summary>
public sealed class BiosFile
{
    /// <summary>
    /// Creates a new instance of the <see cref="BiosFile"/> class.
    /// </summary>
    /// <param name="label">What the file is; doubles as the only name when none follow.</param>
    /// <param name="names">Accepted file names, preferred first, with a forward-slash subfolder where the core wants one, e.g. "neocd/neocd_z.rom".</param>
    public BiosFile(string label, params string[] names)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Label is required.", nameof(label));
        if (names is null)
            throw new ArgumentNullException(nameof(names));
        if (names.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Names cannot be blank.", nameof(names));

        Label = label;
        Names = names.Length == 0 ? new[] { label } : names;
    }

    /// <summary>
    /// What the file is, e.g. "PlayStation BIOS".
    /// </summary>
    public string Label { get; }
    /// <summary>
    /// File names the core accepts, preferred first, subfolder included where one applies.
    /// </summary>
    public IReadOnlyList<string> Names { get; }

    /// <summary>
    /// The accepted name whose file name a picked file already carries, subfolder included, or the preferred one.
    /// </summary>
    /// <param name="path">The picked file.</param>
    public string NameFor(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));
        var own = Path.GetFileName(path);
        return Names.FirstOrDefault(name => string.Equals(Path.GetFileName(name), own, StringComparison.OrdinalIgnoreCase)) ?? Names[0];
    }

    /// <summary>
    /// The accepted name found in a folder, its subfolder resolved under it, or null when none is there.
    /// </summary>
    /// <param name="folder">Folder to look in.</param>
    public string? Present(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
            throw new ArgumentException("Folder is required.", nameof(folder));
        return Names.FirstOrDefault(name => File.Exists(Path.Combine(folder, name)));
    }

    /// <inheritdoc/>
    public override string ToString() => Label;
}
