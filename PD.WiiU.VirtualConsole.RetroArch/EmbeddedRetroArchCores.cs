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
    /// Every core shipped, recommended first within its console.
    /// </summary>
    public static readonly IReadOnlyList<RetroArchCore> All = new[]
    {
        new RetroArchCore("genesis_plus_gx", "Genesis Plus GX", SourceConsole.Genesis, "Most accurate; the usual choice.") { IsRecommended = true },
        new RetroArchCore("genesis_plus_gx_wide", "Genesis Plus GX Wide", SourceConsole.Genesis, "Same emulator with a 16:9 widescreen hack."),
        new RetroArchCore("picodrive", "PicoDrive", SourceConsole.Genesis, "Faster and lighter; less accurate."),
        new RetroArchCore("genesis_plus_gx", "Genesis Plus GX", SourceConsole.MasterSystem, "Most accurate; the usual choice.") { IsRecommended = true },
        new RetroArchCore("gearsystem", "Gearsystem", SourceConsole.MasterSystem, "Lighter Master System and Game Gear emulator."),
        new RetroArchCore("genesis_plus_gx", "Genesis Plus GX", SourceConsole.GameGear, "Most accurate; the usual choice.") { IsRecommended = true },
        new RetroArchCore("gearsystem", "Gearsystem", SourceConsole.GameGear, "Lighter Master System and Game Gear emulator."),
        new RetroArchCore("picodrive", "PicoDrive", SourceConsole.Sega32X, "The one core with 32X support.") { IsRecommended = true },
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
    };

    /// <inheritdoc/>
    public IReadOnlyList<RetroArchCore> Available(SourceConsole console) =>
        All.Where(core => core.Console == console).OrderByDescending(core => core.IsRecommended).ToArray();

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
