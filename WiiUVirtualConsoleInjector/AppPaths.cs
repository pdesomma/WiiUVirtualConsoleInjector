namespace WiiUVirtualConsoleInjector;

/// <summary>
/// Where the application keeps its per-user files.
/// </summary>
public sealed class AppPaths
{
    /// <summary>
    /// Folder name under the local application data folder.
    /// </summary>
    public const string FolderName = "WiiUVirtualConsoleInjector";

    /// <summary>
    /// Creates a new instance of the <see cref="AppPaths"/> class.
    /// </summary>
    /// <param name="dataFolder">Root for settings, keys and caches.</param>
    public AppPaths(string dataFolder)
    {
        if (string.IsNullOrWhiteSpace(dataFolder))
            throw new ArgumentException("Data folder is required.", nameof(dataFolder));

        DataFolder = Path.GetFullPath(dataFolder);
    }

    /// <summary>
    /// Root for settings, keys and caches.
    /// </summary>
    public string DataFolder { get; }
    /// <summary>
    /// Default base store when the user has not chosen one.
    /// </summary>
    public string DefaultBasePath => Path.Combine(DataFolder, "bases");
    /// <summary>
    /// Default output folder when the user has not chosen one.
    /// </summary>
    public string DefaultOutputPath => Path.Combine(DataFolder, "output");
    /// <summary>
    /// Folder the injection history lives in.
    /// </summary>
    public string HistoryFolder => Path.Combine(DataFolder, "history");
    /// <summary>
    /// The user's keys.
    /// </summary>
    public string KeysFile => Path.Combine(DataFolder, "keys.json");
    /// <summary>
    /// The custom base list.
    /// </summary>
    public string CustomBasesFile => Path.Combine(DataFolder, "custom-bases.json");
    /// <summary>
    /// Downloaded packages waiting to be unpacked.
    /// </summary>
    public string PackageCache => Path.Combine(DataFolder, "packages");
    /// <summary>
    /// Saved settings.
    /// </summary>
    public string SettingsFile => Path.Combine(DataFolder, "settings.json");
    /// <summary>
    /// Scratch space for injections when settings name none.
    /// </summary>
    public string WorkFolder => Path.Combine(DataFolder, "work");

    /// <summary>
    /// The per-user folder for this application.
    /// </summary>
    public static AppPaths Default() =>
        new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FolderName));
}
