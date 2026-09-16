using System.Text.Json;
using System.Text.Json.Serialization;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// The known Virtual Console bases: title ID, name, region and console. No keys.
/// </summary>
public sealed class BaseCatalog
{
    private const string ResourceName = "bases.json";
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly Dictionary<TitleId, BaseTitle> _byId;

    /// <summary>
    /// Creates a new instance of the <see cref="BaseCatalog"/> class.
    /// </summary>
    /// <param name="titles">Bases to list; title IDs must be unique.</param>
    /// <exception cref="ArgumentException">Two entries share a title ID.</exception>
    public BaseCatalog(IEnumerable<BaseTitle> titles)
    {
        if (titles is null)
            throw new ArgumentNullException(nameof(titles));

        Titles = titles.ToArray();
        _byId = new Dictionary<TitleId, BaseTitle>();
        foreach (var title in Titles)
        {
            if (title is null)
                throw new ArgumentException("Catalog entries cannot be null.", nameof(titles));
            if (_byId.ContainsKey(title.TitleId))
                throw new ArgumentException($"Title {title.TitleId} is listed twice.", nameof(titles));
            _byId.Add(title.TitleId, title);
        }
    }

    /// <summary>
    /// Every base, in catalog order.
    /// </summary>
    public IReadOnlyList<BaseTitle> Titles { get; }

    /// <summary>
    /// The catalog shipped with the library.
    /// </summary>
    public static BaseCatalog Bundled()
    {
        using var stream = typeof(BaseCatalog).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("Embedded bases.json is missing.");
        return Parse(stream);
    }

    /// <summary>
    /// The base with a title ID, or null.
    /// </summary>
    /// <param name="titleId">Title ID to look up.</param>
    public BaseTitle? Find(TitleId titleId) => _byId.TryGetValue(titleId, out var title) ? title : null;

    /// <summary>
    /// Bases usable for a console. GameCube injects ride on Wii bases, so those come back retagged.
    /// </summary>
    /// <param name="console">Console the ROM is for.</param>
    public IReadOnlyList<BaseTitle> For(SourceConsole console)
    {
        if (console == SourceConsole.GameCube)
            return Titles.Where(t => t.Console == SourceConsole.Wii).Select(t => new BaseTitle(t.TitleId, t.Name, t.Region, SourceConsole.GameCube) { IsRecommended = t.IsRecommended }).ToArray();
        return Titles.Where(t => t.Console == console).ToArray();
    }

    /// <summary>
    /// Reads a catalog file.
    /// </summary>
    /// <param name="path">JSON file path.</param>
    public static BaseCatalog Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));

        using var stream = File.OpenRead(path);
        return Parse(stream);
    }

    /// <summary>
    /// Reads catalog JSON: an array of { titleId, name, region, console, recommended? }.
    /// </summary>
    /// <param name="json">JSON stream.</param>
    /// <exception cref="InvalidDataException">Malformed JSON or an entry with a bad field.</exception>
    public static BaseCatalog Parse(Stream json)
    {
        if (json is null)
            throw new ArgumentNullException(nameof(json));

        Entry[]? entries;
        try
        {
            entries = JsonSerializer.Deserialize<Entry[]>(json, Options);
        }
        catch (JsonException e)
        {
            throw new InvalidDataException("Catalog JSON is malformed: " + e.Message, e);
        }
        if (entries is null)
            throw new InvalidDataException("Catalog JSON is empty.");

        return new BaseCatalog(entries.Select(ToTitle));
    }

    private static BaseTitle ToTitle(Entry entry)
    {
        if (!TitleId.TryParse(entry.TitleId, out var id))
            throw new InvalidDataException($"'{entry.TitleId}' is not a title ID.");
        if (string.IsNullOrWhiteSpace(entry.Name))
            throw new InvalidDataException($"Title {id} has no name.");
        if (entry.Region is null || entry.Console is null)
            throw new InvalidDataException($"Title {id} is missing its region or console.");

        return new BaseTitle(id, entry.Name!, entry.Region.Value, entry.Console.Value) { IsRecommended = entry.Recommended };
    }

    private sealed class Entry
    {
        public SourceConsole? Console { get; set; }
        public string? Name { get; set; }
        public bool Recommended { get; set; }
        public Region? Region { get; set; }
        public string? TitleId { get; set; }
    }
}
