using System.Text.Json;
using System.Text.Json.Serialization;
using PD.WiiU.VirtualConsole.Options;
using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Infrastructure;

/// <summary>
/// Injection history as a folder: index.json plus one sub-folder per record holding its artwork, sound and icon.
/// </summary>
public sealed class JsonInjectionHistory : IInjectionHistory
{
    /// <summary>
    /// Name of the PNG a record's icon is kept as.
    /// </summary>
    public const string IconFileName = "icon.png";
    /// <summary>
    /// Name of the index file.
    /// </summary>
    public const string IndexFileName = "index.json";

    private const string TempSuffix = ".tmp";
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly object _gate = new();
    private List<InjectionRecord>? _records;

    /// <summary>
    /// Creates a new instance of the <see cref="JsonInjectionHistory"/> class.
    /// </summary>
    /// <param name="folder">Folder the history lives in; created on first write.</param>
    public JsonInjectionHistory(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder))
            throw new ArgumentException("Folder is required.", nameof(folder));

        Folder = Path.GetFullPath(folder);
    }

    /// <inheritdoc/>
    public event EventHandler? Changed;

    /// <summary>
    /// Folder the history lives in.
    /// </summary>
    public string Folder { get; }

    private string IndexPath => Path.Combine(Folder, IndexFileName);

    /// <inheritdoc/>
    public InjectionRecord Add(InjectionRecord record, byte[]? iconTga)
    {
        if (record is null)
            throw new ArgumentNullException(nameof(record));

        var home = Path.Combine(Folder, record.Id);
        Directory.CreateDirectory(home);
        var stored = new InjectionRecord(record.Id, record.CreatedAt, record.Console, record.Template, record.RomPath, record.Name, record.Identity)
        {
            Artwork = new Artwork
            {
                Icon = Keep(record.Artwork.Icon, home, ImageSlot.Icon.Name),
                BootTv = Keep(record.Artwork.BootTv, home, ImageSlot.BootTv.Name),
                BootDrc = Keep(record.Artwork.BootDrc, home, ImageSlot.BootDrc.Name),
                BootLogo = Keep(record.Artwork.BootLogo, home, ImageSlot.BootLogo.Name),
            },
            BootSoundPath = Keep(record.BootSoundPath, home, "bootSound"),
            CardFiles = record.CardFiles,
            Format = record.Format,
            GamePad = record.GamePad,
            IconPath = WriteIcon(iconTga, home),
            Options = record.Options,
            OutputDirectory = record.OutputDirectory,
            ProductId = record.ProductId,
            ShortName = record.ShortName,
        };

        lock (_gate)
        {
            var records = Load();
            records.RemoveAll(r => r.Id == stored.Id);
            records.Insert(0, stored);
            Save(records);
        }
        Changed?.Invoke(this, EventArgs.Empty);
        return stored;
    }

    /// <inheritdoc/>
    public IReadOnlyList<InjectionRecord> All()
    {
        lock (_gate)
            return Load().ToArray();
    }

    /// <inheritdoc/>
    public void Remove(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id is required.", nameof(id));

        lock (_gate)
        {
            var records = Load();
            if (records.RemoveAll(r => r.Id == id) == 0)
                return;
            Save(records);
        }
        var home = Path.Combine(Folder, id);
        if (Directory.Exists(home))
            Directory.Delete(home, recursive: true);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Copies a file into the record's folder under a stable name, keeping its extension; a missing source is kept as its path.
    /// </summary>
    /// <param name="source">File to copy, or null.</param>
    /// <param name="home">Record folder.</param>
    /// <param name="stem">File name without extension.</param>
    private static string? Keep(string? source, string home, string stem)
    {
        if (string.IsNullOrWhiteSpace(source))
            return null;
        if (!File.Exists(source))
            return source;

        var target = Path.Combine(home, stem + Path.GetExtension(source));
        File.Copy(source!, target, overwrite: true);
        return target;
    }

    /// <summary>
    /// Writes the shipped icon as PNG; a TGA that cannot be decoded is skipped.
    /// </summary>
    /// <param name="iconTga">TGA bytes, or null.</param>
    /// <param name="home">Record folder.</param>
    private static string? WriteIcon(byte[]? iconTga, string home)
    {
        if (iconTga is null)
            return null;

        try
        {
            var path = Path.Combine(home, IconFileName);
            File.WriteAllBytes(path, TgaPng.Encode(iconTga));
            return path;
        }
        catch (Exception e) when (e is InvalidDataException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>
    /// The records, read from disk once and cached; unreadable index reads as empty.
    /// </summary>
    private List<InjectionRecord> Load()
    {
        if (_records is not null)
            return _records;

        _records = new List<InjectionRecord>();
        if (!File.Exists(IndexPath))
            return _records;

        Document[]? documents;
        try
        {
            using var stream = File.OpenRead(IndexPath);
            documents = JsonSerializer.Deserialize<Document[]>(stream, Options);
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            return _records;
        }

        foreach (var document in documents ?? Array.Empty<Document>())
        {
            if (ToRecord(document) is { } record)
                _records.Add(record);
        }
        return _records;
    }

    /// <summary>
    /// Writes the index atomically.
    /// </summary>
    private void Save(List<InjectionRecord> records)
    {
        Directory.CreateDirectory(Folder);
        var temp = IndexPath + TempSuffix;
        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            JsonSerializer.Serialize(stream, records.Select(ToDocument).ToArray(), Options);
        if (File.Exists(IndexPath))
            File.Delete(IndexPath);
        File.Move(temp, IndexPath);
    }

    private static Document ToDocument(InjectionRecord record) => new()
    {
        Id = record.Id,
        CreatedAt = record.CreatedAt,
        Console = record.Console,
        BaseTitleId = record.Template.BaseTitleId?.ToString(),
        CoreId = record.Template.CoreId,
        RomPath = record.RomPath,
        Name = record.Name,
        ShortName = record.ShortName,
        ProductId = record.ProductId,
        GamePad = record.GamePad,
        Format = record.Format,
        TitleId = record.Identity.TitleId.ToString(),
        GroupId = record.Identity.GroupId.ToString(),
        ProductCode = record.Identity.ProductCode.ToString(),
        Icon = record.Artwork.Icon,
        BootTv = record.Artwork.BootTv,
        BootDrc = record.Artwork.BootDrc,
        BootLogo = record.Artwork.BootLogo,
        BootSound = record.BootSoundPath,
        CardFiles = record.CardFiles.Select(f => new CardFileDocument { Source = f.SourcePath, CardPath = f.CardPath }).ToArray(),
        IconPng = record.IconPath,
        OutputDirectory = record.OutputDirectory,
        Options = record.Options is { } options ? JsonSerializer.SerializeToElement(options, options.GetType(), Options) : null,
    };

    /// <summary>
    /// A record from its document; null when the ids do not parse.
    /// </summary>
    private static InjectionRecord? ToRecord(Document d)
    {
        if (d.Id is null || d.RomPath is null || d.Name is null
            || ReadTemplate(d) is not { } template
            || !TitleId.TryParse(d.TitleId, out var titleId)
            || !GroupId.TryParse(d.GroupId, out var groupId)
            || !ProductCode.TryParse(d.ProductCode, out var productCode))
            return null;

        return new InjectionRecord(d.Id, d.CreatedAt, d.Console, template, d.RomPath, d.Name, new TitleIdentity(titleId, groupId, productCode))
        {
            Artwork = new Artwork { Icon = d.Icon, BootTv = d.BootTv, BootDrc = d.BootDrc, BootLogo = d.BootLogo },
            BootSoundPath = d.BootSound,
            CardFiles = ReadCardFiles(d.CardFiles),
            Format = d.Format,
            GamePad = d.GamePad,
            IconPath = d.IconPng,
            Options = ReadOptions(d.Console, d.Options),
            OutputDirectory = d.OutputDirectory,
            ProductId = d.ProductId,
            ShortName = d.ShortName,
        };
    }

    /// <summary>
    /// The card files that still parse; an entry missing either path is dropped.
    /// </summary>
    private static IReadOnlyList<CardFile> ReadCardFiles(CardFileDocument[]? documents)
    {
        if (documents is null)
            return Array.Empty<CardFile>();
        var files = new List<CardFile>();
        foreach (var d in documents)
        {
            if (string.IsNullOrWhiteSpace(d.Source) || string.IsNullOrWhiteSpace(d.CardPath))
                continue;
            try
            {
                files.Add(new CardFile(d.Source!, d.CardPath!));
            }
            catch (ArgumentException)
            {
            }
        }
        return files;
    }

    /// <summary>
    /// The core when the document names one, else the base; null when neither parses. Older documents only carry a base.
    /// </summary>
    private static TemplateKey? ReadTemplate(Document d)
    {
        if (!string.IsNullOrWhiteSpace(d.CoreId))
            return TemplateKey.Core(d.CoreId!);
        return TitleId.TryParse(d.BaseTitleId, out var baseId) ? TemplateKey.Base(baseId) : null;
    }

    /// <summary>
    /// Options typed by console; anything unreadable reads as defaults.
    /// </summary>
    private static IConsoleOptions? ReadOptions(SourceConsole console, JsonElement? element)
    {
        if (element is not { ValueKind: JsonValueKind.Object } json)
            return null;

        var type = OptionsType(console);
        if (type is null)
            return null;
        try
        {
            return (IConsoleOptions?)json.Deserialize(type, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Type? OptionsType(SourceConsole console) => console switch
    {
        SourceConsole.Nes => typeof(NesOptions),
        SourceConsole.Snes => typeof(SnesOptions),
        SourceConsole.N64 => typeof(N64Options),
        SourceConsole.Gba => typeof(GbaOptions),
        SourceConsole.Nds => typeof(NdsOptions),
        SourceConsole.Wii => typeof(WiiOptions),
        SourceConsole.GameCube => typeof(GameCubeOptions),
        SourceConsole.Arcade => typeof(ArcadeOptions),
        SourceConsole.NeoGeo => typeof(ArcadeOptions),
        _ => null,
    };

    private sealed class CardFileDocument
    {
        public string? CardPath { get; set; }
        public string? Source { get; set; }
    }

    private sealed class Document
    {
        public string? BaseTitleId { get; set; }
        public CardFileDocument[]? CardFiles { get; set; }
        public string? BootDrc { get; set; }
        public string? BootLogo { get; set; }
        public string? BootSound { get; set; }
        public string? BootTv { get; set; }
        public SourceConsole Console { get; set; }
        public string? CoreId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public OutputFormat Format { get; set; }
        public bool GamePad { get; set; }
        public string? GroupId { get; set; }
        public string? Icon { get; set; }
        public string? IconPng { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
        public JsonElement? Options { get; set; }
        public string? OutputDirectory { get; set; }
        public string? ProductCode { get; set; }
        public string? ProductId { get; set; }
        public string? RomPath { get; set; }
        public string? ShortName { get; set; }
        public string? TitleId { get; set; }
    }
}
