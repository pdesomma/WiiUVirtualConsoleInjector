using PD.WiiU.VirtualConsole.Options;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Everything needed to build an injected title again, plus where it went.
/// </summary>
public sealed class InjectionRecord
{
    /// <summary>
    /// Creates a new instance of the <see cref="InjectionRecord"/> class.
    /// </summary>
    /// <param name="id">Unique id; also the record's folder name.</param>
    /// <param name="createdAt">When the inject finished.</param>
    /// <param name="console">Console injected.</param>
    /// <param name="template">Base or core the title was built on.</param>
    /// <param name="romPath">ROM that was injected.</param>
    /// <param name="name">Long name, commas as line breaks.</param>
    /// <param name="identity">IDs the title was given.</param>
    public InjectionRecord(string id, DateTimeOffset createdAt, SourceConsole console, TemplateKey template, string romPath, string name, TitleIdentity identity)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(romPath))
            throw new ArgumentException("ROM path is required.", nameof(romPath));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Id = id;
        CreatedAt = createdAt;
        Console = console;
        Template = template ?? throw new ArgumentNullException(nameof(template));
        RomPath = romPath;
        Name = name;
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
    }

    /// <summary>
    /// Replacement images that were used.
    /// </summary>
    public Artwork Artwork { get; init; } = Artwork.None;
    /// <summary>
    /// Boot sound that was used, or null for the base's.
    /// </summary>
    public string? BootSoundPath { get; init; }
    /// <summary>
    /// Files that went onto the card beside the title, such as a BIOS.
    /// </summary>
    public IReadOnlyList<CardFile> CardFiles { get; init; } = Array.Empty<CardFile>();
    /// <summary>
    /// Console injected.
    /// </summary>
    public SourceConsole Console { get; }
    /// <summary>
    /// When the inject finished.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }
    /// <summary>
    /// How the title was written out.
    /// </summary>
    public OutputFormat Format { get; init; } = OutputFormat.Wup;
    /// <summary>
    /// GamePad-as-controller was requested.
    /// </summary>
    public bool GamePad { get; init; }
    /// <summary>
    /// PNG of the icon the title shipped with, or null when none was captured.
    /// </summary>
    public string? IconPath { get; init; }
    /// <summary>
    /// Unique id; also the record's folder name.
    /// </summary>
    public string Id { get; }
    /// <summary>
    /// IDs the title was given; reused so a rebuild replaces it on the console.
    /// </summary>
    public TitleIdentity Identity { get; }
    /// <summary>
    /// Long name, commas as line breaks.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// Console-specific settings, or null for defaults.
    /// </summary>
    public IConsoleOptions? Options { get; init; }
    /// <summary>
    /// Where the finished title was written.
    /// </summary>
    public string? OutputDirectory { get; init; }
    /// <summary>
    /// Product ID the user typed, or null for the generated one.
    /// </summary>
    public string? ProductId { get; init; }
    /// <summary>
    /// ROM that was injected.
    /// </summary>
    public string RomPath { get; }
    /// <summary>
    /// Short name for the HOME Menu, or null when taken from the long name.
    /// </summary>
    public string? ShortName { get; init; }
    /// <summary>
    /// Base or core the title was built on.
    /// </summary>
    public TemplateKey Template { get; }

    /// <summary>
    /// The first line of the long name; what a tile shows.
    /// </summary>
    public string DisplayName => Name.Split(',')[0].Trim();

    /// <inheritdoc/>
    public override string ToString() => $"{DisplayName} ({Console})";
}
