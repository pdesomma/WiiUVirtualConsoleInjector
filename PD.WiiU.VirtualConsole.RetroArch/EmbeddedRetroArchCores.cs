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
        new RetroArchSystem(SourceConsole.AtariLynx, ".lnx") { BiosFiles = new[] { "lynxboot.img" } },
        new RetroArchSystem(SourceConsole.VirtualBoy, ".vb", ".vboy"),
        new RetroArchSystem(SourceConsole.Arcade, ".zip", ".7z"),
        new RetroArchSystem(SourceConsole.NeoGeo, ".zip", ".7z"),
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
