using System.Text.Json;
using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp.Nus;

namespace PD.WiiU.VirtualConsole.Infrastructure;

/// <summary>
/// The previous application, UWUVCI AIO: settings.json under LocalAppData\UWUVCI-V3, title keys under bin/keys and bases under bin/BaseGames of wherever it was unzipped, Documents\UWUVCI AIO by default.
/// </summary>
public sealed class UwuvciLegacyImport : ILegacyImport
{
    /// <summary>
    /// Folder under LocalAppData that holds settings.json.
    /// </summary>
    public const string SettingsFolderName = "UWUVCI-V3";

    private readonly IReadOnlyList<string> _appFolders;
    private readonly IBaseStore _bases;
    private readonly BaseCatalog _catalog;
    private readonly IKeyStore _keys;
    private readonly string _settingsPath;

    /// <summary>
    /// Creates a new instance of the <see cref="UwuvciLegacyImport"/> class looking in the usual places.
    /// </summary>
    /// <param name="catalog">Bases to recognise.</param>
    /// <param name="keys">Where keys go.</param>
    /// <param name="bases">Where bases go.</param>
    public UwuvciLegacyImport(BaseCatalog catalog, IKeyStore keys, IBaseStore bases)
        : this(catalog, keys, bases, DefaultSettingsPath(), DefaultAppFolders())
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="UwuvciLegacyImport"/> class looking in given places.
    /// </summary>
    /// <param name="catalog">Bases to recognise.</param>
    /// <param name="keys">Where keys go.</param>
    /// <param name="bases">Where bases go.</param>
    /// <param name="settingsPath">The settings.json to read.</param>
    /// <param name="appFolders">Candidate install folders, the ones holding bin.</param>
    public UwuvciLegacyImport(BaseCatalog catalog, IKeyStore keys, IBaseStore bases, string settingsPath, IEnumerable<string> appFolders)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        _bases = bases ?? throw new ArgumentNullException(nameof(bases));
        _settingsPath = settingsPath ?? throw new ArgumentNullException(nameof(settingsPath));
        _appFolders = (appFolders ?? throw new ArgumentNullException(nameof(appFolders))).ToArray();
    }

    /// <summary>
    /// Where the previous application was usually unzipped.
    /// </summary>
    public static IEnumerable<string> DefaultAppFolders()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (documents.Length > 0)
            yield return Path.Combine(documents, "UWUVCI AIO");
    }

    /// <summary>
    /// The settings.json the previous application wrote.
    /// </summary>
    public static string DefaultSettingsPath() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), SettingsFolderName, "settings.json");

    /// <inheritdoc/>
    public LegacyInstall? Find()
    {
        var settings = ReadSettings();
        var app = _appFolders.FirstOrDefault(f => Directory.Exists(Path.Combine(f, "bin")));
        if (settings is null && app is null)
            return null;

        var location = settings is not null ? _settingsPath : app!;
        // the settings may point at a base folder that has since moved; the default one beside bin is looked at too
        var baseFolders = new List<string>();
        if (!string.IsNullOrWhiteSpace(settings?.BasePath))
            baseFolders.Add(settings!.BasePath!);
        if (app is not null)
            baseFolders.Add(Path.Combine(app, "bin", "BaseGames"));
        var suppressed = new List<InjectionWarning>();
        if (settings?.Ndsw == true)
            suppressed.Add(InjectionWarning.NdsDsiEnhanced);
        if (settings?.Snesw == true)
            suppressed.Add(InjectionWarning.SnesCoProcessor);
        if (settings?.Gczw == true)
            suppressed.Add(InjectionWarning.GameCubeGcz);

        var install = new LegacyInstall(location)
        {
            CommonKey = ParseKey(settings?.Ckey),
            OutputFolder = !string.IsNullOrWhiteSpace(settings?.OutPath) && Directory.Exists(settings!.OutPath) ? settings.OutPath : null,
            TitleKeys = app is null ? Array.Empty<LegacyTitleKey>() : ReadTitleKeys(Path.Combine(app, "bin", "keys")),
            Bases = FindBases(baseFolders),
            SuppressedWarnings = suppressed,
        };
        return install.HasAnything ? install : null;
    }

    /// <inheritdoc/>
    public async Task<LegacyImportReport> ImportAsync(LegacyInstall install, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (install is null)
            throw new ArgumentNullException(nameof(install));

        var commonKeyAdded = false;
        if (install.CommonKey is { } common && _keys.CommonKey is null)
        {
            _keys.CommonKey = common;
            commonKeyAdded = true;
        }
        var titleKeys = 0;
        foreach (var key in install.TitleKeys)
        {
            if (_keys.GetTitleKey(key.TitleId) is not null)
                continue;
            _keys.SetTitleKey(key.TitleId, key.Key);
            titleKeys++;
        }
        var bases = 0;
        var failures = new List<string>();
        foreach (var @base in install.Bases)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_bases.Locate(@base.Base).Exists)
                continue;
            try
            {
                progress?.Report("Copying " + @base.Base.Name);
                await _bases.ImportAsync(@base.Base, @base.Folder, null, progress, cancellationToken).ConfigureAwait(false);
                bases++;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                failures.Add($"{@base.Base.Name}: {e.Message}");
            }
        }
        return new LegacyImportReport(commonKeyAdded, titleKeys, bases, failures);
    }

    private static CommonKey? ParseKey(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return null;
        try
        {
            return CommonKey.Parse(hex!.Trim());
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private IReadOnlyList<LegacyBase> FindBases(IEnumerable<string> folders)
    {
        var found = new Dictionary<WiiUSharp.TitleId, LegacyBase>();
        foreach (var folder in folders.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (var sub in Directory.GetDirectories(folder).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                BaseFolderInfo? info;
                try
                {
                    info = BaseFolder.Inspect(sub);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException)
                {
                    continue;
                }
                if (info is { Kind: BaseFolderKind.Title, TitleId: { } id } && !found.ContainsKey(id) && _catalog.Find(id) is { } @base)
                    found[id] = new LegacyBase(@base, sub);
            }
        }
        return found.Values.ToArray();
    }

    private static IReadOnlyList<LegacyTitleKey> ReadTitleKeys(string folder)
    {
        if (!Directory.Exists(folder))
            return Array.Empty<LegacyTitleKey>();
        var keys = new Dictionary<WiiUSharp.TitleId, LegacyTitleKey>();
        foreach (var file in Directory.GetFiles(folder, "*" + UwuvciKeyFile.Extension))
        {
            try
            {
                foreach (var key in UwuvciKeyFile.Read(file))
                    keys[key.TitleId] = key;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException)
            {
            }
        }
        return keys.Values.ToArray();
    }

    private Settings? ReadSettings()
    {
        if (!File.Exists(_settingsPath))
            return null;
        try
        {
            return JsonSerializer.Deserialize<Settings>(File.ReadAllText(_settingsPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    private sealed class Settings
    {
        public string? BasePath { get; set; }
        public string? Ckey { get; set; }
        public bool? Gczw { get; set; }
        public bool? Ndsw { get; set; }
        public string? OutPath { get; set; }
        public bool? Snesw { get; set; }
    }
}
