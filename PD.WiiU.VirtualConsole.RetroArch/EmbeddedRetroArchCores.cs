using PD.WiiU.VirtualConsole.Ports;

namespace PD.WiiU.VirtualConsole.RetroArch;

/// <summary>
/// The cores compiled into this assembly and the consoles they serve, staged into the embedded template. One executable can serve several consoles; it is embedded once, under its own name.
/// </summary>
public sealed class EmbeddedRetroArchCores : IRetroArchCores
{
    /// <summary>
    /// Prefix of every core resource; the executable name follows.
    /// </summary>
    public const string ResourcePrefix = "cores/";

    /// <summary>
    /// Every core shipped, in the order they are offered within each console.
    /// </summary>
    public static readonly IReadOnlyList<RetroArchCore> All = new[]
    {
        new RetroArchCore("genesis_plus_gx", "Genesis Plus GX", SourceConsole.Genesis, "Most accurate; the usual choice."),
        new RetroArchCore("genesis_plus_gx_wide", "Genesis Plus GX Wide", SourceConsole.Genesis, "Same emulator with a 16:9 widescreen hack."),
        new RetroArchCore("picodrive", "PicoDrive", SourceConsole.Genesis, "Faster and lighter; less accurate."),
        new RetroArchCore("genesis_plus_gx", "Genesis Plus GX", SourceConsole.MasterSystem, "Most accurate; the usual choice."),
        new RetroArchCore("gearsystem", "Gearsystem", SourceConsole.MasterSystem, "Lighter Master System and Game Gear emulator."),
        new RetroArchCore("genesis_plus_gx", "Genesis Plus GX", SourceConsole.GameGear, "Most accurate; the usual choice."),
        new RetroArchCore("gearsystem", "Gearsystem", SourceConsole.GameGear, "Lighter Master System and Game Gear emulator."),
        new RetroArchCore("picodrive", "PicoDrive", SourceConsole.Sega32X, "The one core with 32X support."),
        new RetroArchCore("stella2023", "Stella 2023", SourceConsole.Atari2600, "The Stella emulator, 2023 build."),
        new RetroArchCore("prosystem", "ProSystem", SourceConsole.Atari7800, "The ProSystem emulator."),
        new RetroArchCore("handy", "Handy", SourceConsole.AtariLynx, "The Handy emulator; needs lynxboot.img on the card."),
        new RetroArchCore("mednafen_vb", "Beetle VB", SourceConsole.VirtualBoy, "Mednafen's Virtual Boy emulator; the red-on-black view goes to both screens."),
        new RetroArchCore("fbneo", "FinalBurn Neo", SourceConsole.Arcade, "FinalBurn Neo; current FBNeo romsets."),
        new RetroArchCore("mame2003_plus", "MAME 2003-Plus", SourceConsole.Arcade, "MAME 0.78 romsets with later additions."),
        new RetroArchCore("mame2010", "MAME 2010", SourceConsole.Arcade, "MAME 0.139 romsets; heavier."),
        new RetroArchCore("mame2000", "MAME 2000", SourceConsole.Arcade, "MAME 0.37b5 romsets; light and fast."),
        new RetroArchCore("mame2003_midway", "MAME 2003 Midway", SourceConsole.Arcade, "MAME 0.78 Midway subset."),
        new RetroArchCore("fbalpha2012", "FB Alpha 2012", SourceConsole.Arcade, "FB Alpha 0.2.97.29 romsets; superseded by FBNeo."),
        new RetroArchCore("fbalpha2012_cps1", "FB Alpha 2012 CPS-1", SourceConsole.Arcade, "FB Alpha 2012, CPS-1 only."),
        new RetroArchCore("fbalpha2012_cps2", "FB Alpha 2012 CPS-2", SourceConsole.Arcade, "FB Alpha 2012, CPS-2 only."),
        new RetroArchCore("fbalpha2012_cps3", "FB Alpha 2012 CPS-3", SourceConsole.Arcade, "FB Alpha 2012, CPS-3 only."),
        new RetroArchCore("fbalpha2012_neogeo", "FB Alpha 2012 Neo Geo", SourceConsole.Arcade, "FB Alpha 2012, Neo Geo only."),
        new RetroArchCore("fbneo", "FinalBurn Neo", SourceConsole.NeoGeo, "FinalBurn Neo; needs neogeo.zip beside the game."),
        new RetroArchCore("pcsx_rearmed", "PCSX-ReARMed", SourceConsole.PlayStation, "The PlayStation emulator; a BIOS on the card is strongly advised, HLE otherwise."),
        new RetroArchCore("pokemini", "PokeMini", SourceConsole.PokemonMini, "The PokeMini emulator; bios.min on the card is optional."),
        new RetroArchCore("mednafen_ngp", "Beetle NeoPop", SourceConsole.NeoGeoPocket, "Mednafen's Neo Geo Pocket and Color emulator; the usual choice."),
        new RetroArchCore("race", "RACE", SourceConsole.NeoGeoPocket, "RACE; lighter, less accurate."),
        new RetroArchCore("mednafen_wswan", "Beetle WonderSwan", SourceConsole.WonderSwan, "Mednafen's WonderSwan and Color emulator."),
        new RetroArchCore("potator", "Potator", SourceConsole.Supervision, "The Potator Watara Supervision emulator."),
        new RetroArchCore("gw", "GW", SourceConsole.GameAndWatch, "Game & Watch simulator; takes .mgw packs."),
        new RetroArchCore("gearcoleco", "Gearcoleco", SourceConsole.ColecoVision, "The Gearcoleco emulator; needs colecovision.rom on the card."),
        new RetroArchCore("freeintv", "FreeIntv", SourceConsole.Intellivision, "The FreeIntv emulator; needs exec.bin and grom.bin on the card."),
        new RetroArchCore("o2em", "O2EM", SourceConsole.Odyssey2, "Odyssey 2 and Videopac emulator; needs o2rom.bin on the card."),
        new RetroArchCore("vecx", "vecx", SourceConsole.Vectrex, "The vecx Vectrex emulator; no BIOS needed."),
    };

    /// <summary>
    /// Every console a core serves and the ROM files it takes.
    /// </summary>
    public static readonly IReadOnlyList<RetroArchSystem> Systems = new[]
    {
        new RetroArchSystem(SourceConsole.Genesis, ".md", ".bin", ".gen", ".smd", ".68k", ".sgd"),
        new RetroArchSystem(SourceConsole.MasterSystem, ".sms"),
        new RetroArchSystem(SourceConsole.GameGear, ".gg"),
        new RetroArchSystem(SourceConsole.Sega32X, ".32x", ".bin"),
        new RetroArchSystem(SourceConsole.Atari2600, ".a26", ".bin"),
        new RetroArchSystem(SourceConsole.Atari7800, ".a78", ".bin"),
        new RetroArchSystem(SourceConsole.AtariLynx, ".lnx") { BiosFiles = new[] { new BiosFile("lynxboot.img") } },
        new RetroArchSystem(SourceConsole.VirtualBoy, ".vb", ".vboy"),
        new RetroArchSystem(SourceConsole.Arcade, ".zip", ".7z"),
        new RetroArchSystem(SourceConsole.NeoGeo, ".zip", ".7z"),
        new RetroArchSystem(SourceConsole.PlayStation, ".cue", ".chd", ".pbp", ".m3u", ".iso", ".img") { BiosFiles = new[] { new BiosFile("PlayStation BIOS", "scph5501.bin", "scph5500.bin", "scph5502.bin", "scph1001.bin", "psxonpsp660.bin") } },
        new RetroArchSystem(SourceConsole.PokemonMini, ".min"),
        new RetroArchSystem(SourceConsole.NeoGeoPocket, ".ngp", ".ngc", ".ngpc", ".npc"),
        new RetroArchSystem(SourceConsole.WonderSwan, ".ws", ".wsc", ".pc2", ".pcv2"),
        new RetroArchSystem(SourceConsole.Supervision, ".bin", ".sv"),
        new RetroArchSystem(SourceConsole.GameAndWatch, ".mgw"),
        new RetroArchSystem(SourceConsole.ColecoVision, ".col", ".cv", ".bin", ".rom") { BiosFiles = new[] { new BiosFile("colecovision.rom") } },
        new RetroArchSystem(SourceConsole.Intellivision, ".int", ".bin", ".rom") { BiosFiles = new[] { new BiosFile("exec.bin"), new BiosFile("grom.bin") } },
        new RetroArchSystem(SourceConsole.Odyssey2, ".bin") { BiosFiles = new[] { new BiosFile("o2rom.bin") } },
        new RetroArchSystem(SourceConsole.Vectrex, ".bin", ".vec"),
    };

    /// <inheritdoc/>
    public IReadOnlyList<RetroArchCore> Available(SourceConsole console) =>
        All.Where(core => core.Console == console).ToArray();

    /// <summary>
    /// Logical name of a core's executable resource.
    /// </summary>
    /// <param name="core">The core.</param>
    public static string ResourceName(RetroArchCore core)
    {
        if (core is null)
            throw new ArgumentNullException(nameof(core));
        return ResourcePrefix + core.RpxFileName;
    }

    /// <inheritdoc/>
    public RetroArchSystem? System(SourceConsole console) => Systems.FirstOrDefault(s => s.Console == console);

    /// <inheritdoc/>
    /// <exception cref="ArgumentException">The core is not one that ships.</exception>
    public async Task<TitleDirectory> StageAsync(RetroArchCore core, string destination, CancellationToken cancellationToken = default)
    {
        if (core is null)
            throw new ArgumentNullException(nameof(core));
        if (string.IsNullOrWhiteSpace(destination))
            throw new ArgumentException("Destination is required.", nameof(destination));
        var resource = ResourceName(core);
        if (!EmbeddedResources.Exists(resource))
            throw new ArgumentException($"{core.Name} is not a bundled core.", nameof(core));

        var title = new TitleDirectory(destination);
        await RetroArchTemplate.WriteAsync(title, cancellationToken).ConfigureAwait(false);
        using var source = EmbeddedResources.Open(resource);
        using var rpx = new FileStream(Path.Combine(title.Code, core.RpxFileName), FileMode.Create, FileAccess.Write, FileShare.None);
        await source.CopyToAsync(rpx, 81920, cancellationToken).ConfigureAwait(false);
        return title;
    }
}
