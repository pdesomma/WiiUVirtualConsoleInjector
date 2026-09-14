using PD.WiiU.VirtualConsole.Options;
using PD.WiiU.VirtualConsole.Ports;

namespace PD.WiiU.VirtualConsole.Retro;

/// <summary>
/// Injects an N64 ROM into a Virtual Console base: content/rom, the emulator INI and the frame layout.
/// </summary>
public sealed class N64RomInjector : IRomInjector
{
    /// <summary>
    /// Folder under content holding the per-ROM INI.
    /// </summary>
    public const string ConfigFolder = "config";
    /// <summary>
    /// Folder under content holding the single ROM.
    /// </summary>
    public const string RomFolder = "rom";

    /// <inheritdoc/>
    public SourceConsole Console => SourceConsole.N64;

    /// <inheritdoc/>
    /// <exception cref="FileNotFoundException">The base has no content/rom file.</exception>
    public Task InjectAsync(Injection injection, TitleDirectory title, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (injection is null)
            throw new ArgumentNullException(nameof(injection));
        if (title is null)
            throw new ArgumentNullException(nameof(title));
        if (injection.Console != SourceConsole.N64)
            throw new ArgumentException($"Injection is for {injection.Console}.", nameof(injection));

        var options = injection.Options as N64Options ?? new N64Options();
        var romPath = LocateRom(title);

        progress?.Report($"Injecting {Path.GetFileName(injection.Rom.Path)}");
        File.WriteAllBytes(romPath, N64Rom.ToBigEndian(File.ReadAllBytes(injection.Rom.Path)));
        cancellationToken.ThrowIfCancellationRequested();

        var ini = Path.Combine(title.Content, ConfigFolder, Path.GetFileName(romPath) + ".ini");
        progress?.Report(options.IniPath is null ? "Writing empty INI" : $"Copying {Path.GetFileName(options.IniPath)}");
        Directory.CreateDirectory(Path.GetDirectoryName(ini)!);
        if (options.IniPath is null)
            File.WriteAllBytes(ini, Array.Empty<byte>());
        else
            File.Copy(options.IniPath, ini, overwrite: true);

        if (options.WideScreen || options.RemoveDarkFilter)
        {
            progress?.Report("Patching " + FrameLayoutPatch.FileName);
            var layoutPath = Path.Combine(title.Content, FrameLayoutPatch.FileName);
            var archive = File.ReadAllBytes(layoutPath);
            FrameLayoutPatch.Apply(archive, options.WideScreen, options.RemoveDarkFilter);
            File.WriteAllBytes(layoutPath, archive);
        }
        return Task.CompletedTask;
    }

    private static string LocateRom(TitleDirectory title)
    {
        var folder = Path.Combine(title.Content, RomFolder);
        var files = Directory.Exists(folder) ? Directory.GetFiles(folder) : Array.Empty<string>();
        if (files.Length != 1)
            throw new FileNotFoundException($"Expected exactly one file in {folder}, found {files.Length}.");
        return files[0];
    }
}
