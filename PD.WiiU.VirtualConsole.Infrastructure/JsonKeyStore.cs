using System.Text.Json;
using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp;
using WiiUSharp.Nus;

namespace PD.WiiU.VirtualConsole.Infrastructure;

/// <summary>
/// Keys kept in one JSON file: { "commonKey": hex | null, "titleKeys": { titleId: hex } }.
/// </summary>
public sealed class JsonKeyStore : IKeyStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private AncastKey? _ancastKey;
    private CommonKey? _commonKey;
    private Dictionary<TitleId, EncryptedTitleKey>? _titleKeys;

    /// <summary>
    /// Creates a new instance of the <see cref="JsonKeyStore"/> class.
    /// </summary>
    /// <param name="path">JSON file; need not exist yet.</param>
    public JsonKeyStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));

        FilePath = Path.GetFullPath(path);
    }

    /// <inheritdoc/>
    public AncastKey? AncastKey
    {
        get
        {
            Load();
            return _ancastKey;
        }
        set
        {
            Load();
            _ancastKey = value;
            Save();
        }
    }

    /// <inheritdoc/>
    public CommonKey? CommonKey
    {
        get
        {
            Load();
            return _commonKey;
        }
        set
        {
            Load();
            _commonKey = value;
            Save();
        }
    }

    /// <summary>
    /// JSON file the keys live in.
    /// </summary>
    public string FilePath { get; }

    /// <inheritdoc/>
    public EncryptedTitleKey? GetTitleKey(TitleId titleId)
    {
        Load();
        return _titleKeys!.TryGetValue(titleId, out var key) ? key : null;
    }

    /// <inheritdoc/>
    public void SetTitleKey(TitleId titleId, EncryptedTitleKey? titleKey)
    {
        Load();
        if (titleKey is null)
            _titleKeys!.Remove(titleId);
        else
            _titleKeys![titleId] = titleKey.Value;
        Save();
    }

    /// <summary>
    /// Reads the file once; a missing file is an empty store.
    /// </summary>
    /// <exception cref="InvalidDataException">The file is not valid key JSON.</exception>
    private void Load()
    {
        if (_titleKeys is not null)
            return;

        var document = File.Exists(FilePath) ? Read() : new Document();
        var titleKeys = new Dictionary<TitleId, EncryptedTitleKey>();
        try
        {
            foreach (var pair in document.TitleKeys ?? new Dictionary<string, string?>())
                titleKeys[TitleId.Parse(pair.Key)] = EncryptedTitleKey.Parse(pair.Value!);
            _commonKey = document.CommonKey is null ? null : WiiUSharp.Nus.CommonKey.Parse(document.CommonKey);
            _ancastKey = document.AncastKey is null ? null : VirtualConsole.AncastKey.Parse(document.AncastKey);
        }
        catch (Exception e) when (e is FormatException or ArgumentNullException or OverflowException)
        {
            throw new InvalidDataException($"{FilePath} holds a malformed key: {e.Message}", e);
        }
        _titleKeys = titleKeys;
    }

    /// <summary>
    /// Parses the file into its raw form.
    /// </summary>
    /// <exception cref="InvalidDataException">Malformed JSON.</exception>
    private Document Read()
    {
        try
        {
            using var stream = File.OpenRead(FilePath);
            return JsonSerializer.Deserialize<Document>(stream, Options) ?? throw new InvalidDataException($"{FilePath} is empty.");
        }
        catch (JsonException e)
        {
            throw new InvalidDataException($"{FilePath} is malformed: {e.Message}", e);
        }
    }

    /// <summary>
    /// Writes the whole store, creating its folder as needed.
    /// </summary>
    private void Save()
    {
        var document = new Document
        {
            AncastKey = _ancastKey?.ToString(),
            CommonKey = _commonKey?.ToString(),
            TitleKeys = _titleKeys!.ToDictionary(p => p.Key.ToString(), p => (string?)p.Value.ToString()),
        };
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(document, Options));
    }

    private sealed class Document
    {
        public string? AncastKey { get; set; }
        public string? CommonKey { get; set; }
        public Dictionary<string, string?>? TitleKeys { get; set; }
    }
}
