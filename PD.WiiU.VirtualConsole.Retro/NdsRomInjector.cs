using System.IO.Compression;
using System.Text.Json.Nodes;
using PD.WiiU.VirtualConsole.Options;
using PD.WiiU.VirtualConsole.Ports;

namespace PD.WiiU.VirtualConsole.Retro;

/// <summary>
/// Injects a DS ROM into a Virtual Console base: content/0010/rom.zip, display settings and optional layout screens.
/// </summary>
public sealed class NdsRomInjector : IRomInjector
{
    /// <summary>
    /// Settings file next to the ROM archive.
    /// </summary>
    public const string ConfigurationFileName = "configuration_cafe.json";
    /// <summary>
    /// Folder under content holding the ROM archive and settings.
    /// </summary>
    public const string DataFolder = "0010";
    /// <summary>
    /// Archive holding the single ROM.
    /// </summary>
    public const string RomArchiveName = "rom.zip";

    /// <inheritdoc/>
    public SourceConsole Console => SourceConsole.Nds;

    /// <inheritdoc/>
    public IReadOnlyList<BaseIssue> Inspect(TitleDirectory title)
    {
        if (title is null)
            throw new ArgumentNullException(nameof(title));

        var inspection = new BaseInspection(title);
        var archive = TitleDirectory.ContentFolder + "/" + DataFolder + "/" + RomArchiveName;
        if (inspection.RequireFile(archive, 1))
            inspection.Parse(archive, () => RomEntryName(Path.Combine(title.Content, DataFolder, RomArchiveName)));
        inspection.RequireFile(TitleDirectory.ContentFolder + "/" + DataFolder + "/" + ConfigurationFileName, 1);
        return inspection.Issues;
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidDataException">rom.zip has no WUP-named entry.</exception>
    public Task InjectAsync(Injection injection, TitleDirectory title, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (injection is null)
            throw new ArgumentNullException(nameof(injection));
        if (title is null)
            throw new ArgumentNullException(nameof(title));
        if (injection.Console != SourceConsole.Nds)
            throw new ArgumentException($"Injection is for {injection.Console}.", nameof(injection));

        var options = injection.Options as NdsOptions ?? new NdsOptions();
        var data = Path.Combine(title.Content, DataFolder);
        var archive = Path.Combine(data, RomArchiveName);
        if (!File.Exists(archive))
            throw new FileNotFoundException("The base has no rom.zip.", archive);

        var entryName = RomEntryName(archive);
        progress?.Report($"Injecting {Path.GetFileName(injection.Rom.Path)} as {entryName}");
        cancellationToken.ThrowIfCancellationRequested();
        using (var stream = new FileStream(archive, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
            zip.CreateEntryFromFile(injection.Rom.Path, entryName, CompressionLevel.Optimal);

        if (options.LayoutScreensPath is not null)
        {
            progress?.Report("Copying layout screens");
            CopyTree(options.LayoutScreensPath, title.Root);
        }

        if (options.Brightness != NdsOptions.DefaultBrightness || options.PixelArtUpscaler != 0)
        {
            progress?.Report("Updating " + ConfigurationFileName);
            UpdateConfiguration(Path.Combine(data, ConfigurationFileName), options);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Name of the entry the base stores its ROM under; the one containing WUP.
    /// </summary>
    /// <param name="archivePath">rom.zip path.</param>
    public static string RomEntryName(string archivePath)
    {
        using var zip = ZipFile.OpenRead(archivePath);
        return zip.Entries.FirstOrDefault(e => e.Name.IndexOf("WUP", StringComparison.Ordinal) >= 0)?.Name
            ?? throw new InvalidDataException("rom.zip has no WUP-named entry.");
    }

    /// <summary>
    /// Writes Brightness and PixelArtUpscaler under configuration.Display.
    /// </summary>
    /// <param name="path">configuration_cafe.json path.</param>
    /// <param name="options">Values to write.</param>
    public static void UpdateConfiguration(string path, NdsOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        var root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject
            ?? throw new InvalidDataException("configuration_cafe.json is not a JSON object.");
        var display = root["configuration"]?["Display"] as JsonObject
            ?? throw new InvalidDataException("configuration_cafe.json has no configuration.Display object.");
        display["Brightness"] = options.Brightness;
        display["PixelArtUpscaler"] = options.PixelArtUpscaler;
        File.WriteAllText(path, root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }

    private static void CopyTree(string source, string destination)
    {
        if (!Directory.Exists(source))
            throw new DirectoryNotFoundException(source);

        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.GetDirectories(source))
            CopyTree(directory, Path.Combine(destination, Path.GetFileName(directory)));
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
    }
}
