using System.Text.Json;
using System.Text.Json.Serialization;
using PD.WiiU.VirtualConsole.Ports;

namespace PD.WiiU.VirtualConsole.Infrastructure;

/// <summary>
/// <see cref="AppSettings"/> as a JSON file; a missing or unreadable file reads as defaults.
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private const string TempSuffix = ".tmp";
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Creates a new instance of the <see cref="JsonSettingsStore"/> class.
    /// </summary>
    /// <param name="path">Settings file; created on first save.</param>
    public JsonSettingsStore(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));

        FilePath = Path.GetFullPath(path);
    }

    /// <summary>
    /// Full path of the settings file.
    /// </summary>
    public string FilePath { get; }

    /// <inheritdoc/>
    public AppSettings Load()
    {
        if (!File.Exists(FilePath))
            return new AppSettings();

        Document? document;
        try
        {
            using var stream = File.OpenRead(FilePath);
            document = JsonSerializer.Deserialize<Document>(stream, Options);
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            return new AppSettings();
        }
        if (document is null)
            return new AppSettings();

        return new AppSettings
        {
            BasePath = Blank(document.BasePath),
            CaptionFontPath = Blank(document.CaptionFontPath),
            CopyToSdCard = document.CopyToSdCard ?? false,
            OutputPath = Blank(document.OutputPath),
            SdPath = Blank(document.SdPath),
            WorkPath = Blank(document.WorkPath),
            SuppressedWarnings = document.SuppressedWarnings ?? Array.Empty<InjectionWarning>(),
            Theme = document.Theme ?? AppTheme.Light,
        };
    }

    /// <inheritdoc/>
    public void Save(AppSettings settings)
    {
        if (settings is null)
            throw new ArgumentNullException(nameof(settings));

        var document = new Document
        {
            BasePath = settings.BasePath,
            CaptionFontPath = settings.CaptionFontPath,
            CopyToSdCard = settings.CopyToSdCard,
            OutputPath = settings.OutputPath,
            SdPath = settings.SdPath,
            WorkPath = settings.WorkPath,
            SuppressedWarnings = settings.SuppressedWarnings.ToArray(),
            Theme = settings.Theme,
        };
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var temp = FilePath + TempSuffix;
        File.WriteAllText(temp, JsonSerializer.Serialize(document, Options));
        if (File.Exists(FilePath))
            File.Delete(FilePath);
        File.Move(temp, FilePath);
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private sealed class Document
    {
        public string? BasePath { get; set; }
        public string? CaptionFontPath { get; set; }
        public bool? CopyToSdCard { get; set; }
        public string? OutputPath { get; set; }
        public string? SdPath { get; set; }
        public InjectionWarning[]? SuppressedWarnings { get; set; }
        public AppTheme? Theme { get; set; }
        public string? WorkPath { get; set; }
    }
}
