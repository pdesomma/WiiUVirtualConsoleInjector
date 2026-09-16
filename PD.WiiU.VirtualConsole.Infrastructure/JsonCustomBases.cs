using System.Text.Json;
using System.Text.Json.Serialization;
using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Infrastructure;

/// <summary>
/// Custom bases in one JSON file, read on every call so another instance's edits show up.
/// </summary>
public sealed class JsonCustomBases : ICustomBases
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Creates a new instance of the <see cref="JsonCustomBases"/> class.
    /// </summary>
    /// <param name="path">The JSON file; created on the first add.</param>
    public JsonCustomBases(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));

        FilePath = Path.GetFullPath(path);
    }

    /// <summary>
    /// The JSON file.
    /// </summary>
    public string FilePath { get; }

    /// <inheritdoc/>
    public void Add(BaseTitle @base)
    {
        if (@base is null)
            throw new ArgumentNullException(nameof(@base));

        var entries = Load();
        entries.RemoveAll(e => e.TitleId == @base.TitleId.ToString());
        entries.Add(new Entry { TitleId = @base.TitleId.ToString(), Name = @base.Name, Region = @base.Region, Console = @base.Console });
        Save(entries);
    }

    /// <inheritdoc/>
    public IReadOnlyList<BaseTitle> All() =>
        Load().Where(e => TitleId.TryParse(e.TitleId, out _) && !string.IsNullOrWhiteSpace(e.Name))
            .Select(e => new BaseTitle(TitleId.Parse(e.TitleId!), e.Name!, e.Region, e.Console) { IsCustom = true })
            .ToList();

    /// <inheritdoc/>
    public bool Remove(TitleId titleId)
    {
        var entries = Load();
        var removed = entries.RemoveAll(e => e.TitleId == titleId.ToString()) > 0;
        if (removed)
            Save(entries);
        return removed;
    }

    private List<Entry> Load()
    {
        if (!File.Exists(FilePath))
            return new List<Entry>();
        try
        {
            return JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(FilePath), Options) ?? new List<Entry>();
        }
        catch (JsonException)
        {
            return new List<Entry>();
        }
    }

    private void Save(List<Entry> entries)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(entries, Options));
    }

    private sealed class Entry
    {
        public SourceConsole Console { get; set; }
        public string? Name { get; set; }
        public Region Region { get; set; }
        public string? TitleId { get; set; }
    }
}
