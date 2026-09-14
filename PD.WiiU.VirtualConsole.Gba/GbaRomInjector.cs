using PD.WiiU.VirtualConsole.Options;
using PD.WiiU.VirtualConsole.Ports;

namespace PD.WiiU.VirtualConsole.Gba;

/// <summary>
/// Injects a GBA ROM, or a Game Boy ROM via Goomba, into the alldata archive of a GBA Virtual Console base.
/// </summary>
public sealed class GbaRomInjector : IRomInjector
{
    private readonly byte[]? _goomba;

    /// <summary>
    /// Creates a new instance of the <see cref="GbaRomInjector"/> class.
    /// </summary>
    /// <param name="goombaPath">Goomba build to wrap Game Boy ROMs with; the embedded one when null.</param>
    public GbaRomInjector(string? goombaPath = null)
    {
        _goomba = goombaPath is null ? null : File.ReadAllBytes(goombaPath);
    }

    /// <inheritdoc/>
    public SourceConsole Console => SourceConsole.Gba;

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
    /// <exception cref="InvalidDataException">The archive holds no .gba file, or more than one.</exception>
    public Task InjectAsync(Injection injection, TitleDirectory title, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (injection is null)
            throw new ArgumentNullException(nameof(injection));
        if (title is null)
            throw new ArgumentNullException(nameof(title));
        if (injection.Console != SourceConsole.Gba)
            throw new ArgumentException($"Injection is for {injection.Console}.", nameof(injection));

        var options = injection.Options as GbaOptions ?? new GbaOptions();
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
            File.WriteAllBytes(target, rom);
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
        var roms = Directory.GetFiles(extracted, "*.gba", SearchOption.AllDirectories);
        if (roms.Length != 1)
            throw new InvalidDataException(roms.Length == 0 ? "The archive holds no .gba file." : $"The archive holds {roms.Length} .gba files.");
        return roms[0];
    }
}
