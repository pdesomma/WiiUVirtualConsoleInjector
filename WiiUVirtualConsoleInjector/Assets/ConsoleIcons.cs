using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PD.WiiU.VirtualConsole;

namespace WiiUVirtualConsoleInjector.Assets;

/// <summary>
/// The console logos shipped as resources, white and near-black, one per <see cref="SourceConsole"/>.
/// </summary>
public static class ConsoleIcons
{
    private const string DarkRoot = "avares://WiiUVirtualConsoleInjector/Assets/Consoles/Dark/";
    private const string Root = "avares://WiiUVirtualConsoleInjector/Assets/Consoles/";

    private static readonly Dictionary<SourceConsole, Bitmap> Cache = new();
    private static readonly Dictionary<SourceConsole, Bitmap> DarkCache = new();

    /// <summary>
    /// Loads (once) the near-black logo bitmap for a console, for light tiles.
    /// </summary>
    /// <param name="console">Console to show.</param>
    public static Bitmap DarkFor(SourceConsole console) => Load(DarkCache, console, DarkUriFor(console));

    /// <summary>
    /// Resource URI of a console's near-black logo.
    /// </summary>
    /// <param name="console">Console to show.</param>
    public static Uri DarkUriFor(SourceConsole console) => new(DarkRoot + console + ".png");

    /// <summary>
    /// Human label for a console.
    /// </summary>
    /// <param name="console">Console to name.</param>
    public static string DisplayName(SourceConsole console) => console switch
    {
        SourceConsole.Nes => "NES",
        SourceConsole.Snes => "SNES",
        SourceConsole.N64 => "Nintendo 64",
        SourceConsole.Gba => "Game Boy Advance",
        SourceConsole.Nds => "Nintendo DS",
        SourceConsole.Tg16 => "TurboGrafx-16",
        SourceConsole.Msx => "MSX",
        SourceConsole.Wii => "Wii",
        SourceConsole.GameCube => "GameCube",
        SourceConsole.Genesis => "Sega Genesis",
        SourceConsole.MasterSystem => "Master System",
        SourceConsole.GameGear => "Game Gear",
        SourceConsole.Sega32X => "Sega 32X",
        _ => console.ToString(),
    };

    /// <summary>
    /// A note shown under the tile, or null: which consoles need a particular firmware.
    /// </summary>
    /// <param name="console">Console to annotate.</param>
    public static string? Caption(SourceConsole console) => console switch
    {
        SourceConsole.Genesis or SourceConsole.MasterSystem or SourceConsole.GameGear or SourceConsole.Sega32X => "Aroma only",
        _ => null,
    };

    /// <summary>
    /// Loads (once) the white logo bitmap for a console.
    /// </summary>
    /// <param name="console">Console to show.</param>
    public static Bitmap For(SourceConsole console) => Load(Cache, console, UriFor(console));

    /// <summary>
    /// Resource URI of a console's white logo.
    /// </summary>
    /// <param name="console">Console to show.</param>
    public static Uri UriFor(SourceConsole console) => new(Root + console + ".png");

    /// <summary>
    /// Reads a logo into the cache on first use.
    /// </summary>
    /// <param name="cache">Cache for the variant.</param>
    /// <param name="console">Console to show.</param>
    /// <param name="uri">Resource to read.</param>
    private static Bitmap Load(Dictionary<SourceConsole, Bitmap> cache, SourceConsole console, Uri uri)
    {
        lock (cache)
        {
            if (!cache.TryGetValue(console, out var bitmap))
            {
                using var stream = AssetLoader.Open(uri);
                bitmap = new Bitmap(stream);
                cache[console] = bitmap;
            }
            return bitmap;
        }
    }
}
