using PD.WiiU.VirtualConsole.Options;
using PD.WiiU.VirtualConsole.Ports;

namespace PD.WiiU.VirtualConsole.Gba;

/// <summary>
/// Injects a GBA ROM, or a Game Boy ROM via Goomba, into the alldata archive of a GBA Virtual Console base. One instance serves either the GBA or the Game Boy console; both ride on the same bases.
/// </summary>
public sealed class GbaRomInjector : IRomInjector
{
    private readonly byte[]? _goomba;

    /// <summary>
    /// Creates a new instance of the <see cref="GbaRomInjector"/> class.
    /// </summary>
    /// <param name="goombaPath">Goomba build to wrap Game Boy ROMs with; the embedded one when null.</param>
    /// <param name="console">Console this instance serves: <see cref="SourceConsole.Gba"/> or <see cref="SourceConsole.GameBoy"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">A console the GBA base cannot run.</exception>
    public GbaRomInjector(string? goombaPath = null, SourceConsole console = SourceConsole.Gba)
    {
        if (console is not (SourceConsole.Gba or SourceConsole.GameBoy))
            throw new ArgumentOutOfRangeException(nameof(console), console, "A GBA base takes GBA and Game Boy ROMs only.");

        _goomba = goombaPath is null ? null : File.ReadAllBytes(goombaPath);
        Console = console;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="GbaRomInjector"/> class for a console, with the embedded Goomba.
    /// </summary>
    /// <param name="console">Console this instance serves: <see cref="SourceConsole.Gba"/> or <see cref="SourceConsole.GameBoy"/>.</param>
    public GbaRomInjector(SourceConsole console)
        : this(null, console)
    {
    }

    /// <inheritdoc/>
    public SourceConsole Console { get; }
    /// <inheritdoc/>
    public TitleKind Kind => TitleKind.VirtualConsole;

    /// <inheritdoc/>
    public IReadOnlyList<BaseIssue> Inspect(TitleDirectory title)
    {
        if (title is null)
            throw new ArgumentNullException(nameof(title));

        var inspection = new BaseInspection(title);
        inspection.RequireFile(TitleDirectory.ContentFolder + "/" + AllDataArchive.ManifestFileName, 1);
        inspection.RequireFile(TitleDirectory.ContentFolder + "/" + AllDataArchive.BaseName + ".bin", 1);
        return inspection.Issues;
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidDataException">No single ROM under system/roms, and no single .gba anywhere.</exception>
    public Task InjectAsync(Injection injection, TitleDirectory title, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (injection is null)
            throw new ArgumentNullException(nameof(injection));
        if (title is null)
            throw new ArgumentNullException(nameof(title));
        if (injection.Console != Console)
            throw new ArgumentException($"Injection is for {injection.Console}.", nameof(injection));

        var options = injection.Options as GbaOptions ?? new GbaOptions { Console = Console };
        var rom = File.ReadAllBytes(injection.Rom.Path);
        if (GoombaRom.IsGameBoy(injection.Rom.Path))
        {
            progress?.Report("Wrapping Game Boy ROM in Goomba");
            rom = GoombaRom.Wrap(rom, _goomba);
        }
        if (options.PokemonPatch)
            progress?.Report(PokemonPatch.Apply(rom) ? "Applied the Pokemon save patch" : "Pokemon save patch: pattern not found, ROM left as is");
        cancellationToken.ThrowIfCancellationRequested();

        progress?.Report("Rewriting alldata archive");
        AllDataArchive.Rewrite(title.Content, Path.Combine(title.Root, ".alldata"), extracted =>
        {
            var target = LocateRom(extracted);
            AllDataArchive.WriteEntry(target, rom);
            if (!options.RemoveDarkFilter)
                return;

            progress?.Report("Removing dark filter");
            var profile = Directory.GetFiles(extracted, AllDataArchive.TitleProfileName, SearchOption.AllDirectories).FirstOrDefault()
                ?? throw new InvalidDataException("The archive has no title_prof.psb.m.");
            AllDataArchive.SetFullBrightness(profile);
        });
        return Task.CompletedTask;
    }

    private static string LocateRom(string extracted)
    {
        // retail bases keep one MDF-compressed ROM under system/roms; a plain .gba anywhere is accepted too
        var folder = Path.Combine(extracted, AllDataArchive.RomFolder.Replace('/', Path.DirectorySeparatorChar));
        var roms = Directory.Exists(folder) ? Directory.GetFiles(folder) : Array.Empty<string>();
        if (roms.Length == 0)
            roms = Directory.GetFiles(extracted, "*.gba", SearchOption.AllDirectories);
        if (roms.Length != 1)
            throw new InvalidDataException(roms.Length == 0 ? "The archive holds no ROM (nothing under system/roms and no .gba)." : $"The archive holds {roms.Length} ROM files.");
        return roms[0];
    }
}
