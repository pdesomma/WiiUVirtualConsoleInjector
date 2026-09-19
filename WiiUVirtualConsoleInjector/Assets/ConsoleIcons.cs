using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PD.WiiU.VirtualConsole;

namespace WiiUVirtualConsoleInjector.Assets;

/// <summary>
/// The console and company logos shipped as resources, white and near-black, one per <see cref="SourceConsole"/> and per company.
/// </summary>
public static class ConsoleIcons
{
    private const string DarkRoot = "avares://WiiUVirtualConsoleInjector/Assets/Consoles/Dark/";
    private const string Root = "avares://WiiUVirtualConsoleInjector/Assets/Consoles/";

    private static readonly Dictionary<string, Bitmap> Cache = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Bitmap> DarkCache = new(StringComparer.Ordinal);

    /// <summary>
    /// A note shown under the tile, or null: which consoles need a particular firmware.
    /// </summary>
    /// <param name="console">Console to annotate.</param>
    public static string? Caption(SourceConsole console) => console switch
    {
        SourceConsole.Genesis or SourceConsole.MasterSystem or SourceConsole.GameGear or SourceConsole.Sega32X
            or SourceConsole.Atari2600 or SourceConsole.Atari7800 or SourceConsole.AtariLynx or SourceConsole.VirtualBoy
            or SourceConsole.PlayStation or SourceConsole.Arcade or SourceConsole.NeoGeo
            or SourceConsole.PokemonMini or SourceConsole.NeoGeoPocket or SourceConsole.WonderSwan or SourceConsole.Supervision
            or SourceConsole.GameAndWatch or SourceConsole.ColecoVision or SourceConsole.Intellivision or SourceConsole.Odyssey2
            or SourceConsole.Vectrex => "Aroma only",
        _ => null,
    };

    /// <summary>
    /// Loads (once) the near-black logo bitmap for a console, for light tiles.
    /// </summary>
    /// <param name="console">Console to show.</param>
    public static Bitmap DarkFor(SourceConsole console) => DarkFor(console.ToString());

    /// <summary>
    /// Loads (once) the near-black logo bitmap by file name, for light tiles.
    /// </summary>
    /// <param name="name">Console or company name, as the file is called.</param>
    public static Bitmap DarkFor(string name) => Load(DarkCache, name, DarkUriFor(name));

    /// <summary>
    /// Resource URI of a console's near-black logo.
    /// </summary>
    /// <param name="console">Console to show.</param>
    public static Uri DarkUriFor(SourceConsole console) => DarkUriFor(console.ToString());

    /// <summary>
    /// Resource URI of a near-black logo by file name.
    /// </summary>
    /// <param name="name">Console or company name.</param>
    public static Uri DarkUriFor(string name) => new(DarkRoot + name + ".png");

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
        SourceConsole.Atari2600 => "Atari 2600",
        SourceConsole.Atari7800 => "Atari 7800",
        SourceConsole.AtariLynx => "Atari Lynx",
        SourceConsole.VirtualBoy => "Virtual Boy",
        SourceConsole.PlayStation => "PlayStation",
        SourceConsole.Arcade => "Arcade",
        SourceConsole.NeoGeo => "Neo Geo",
        SourceConsole.PokemonMini => "Pokémon Mini",
        SourceConsole.NeoGeoPocket => "Neo Geo Pocket",
        SourceConsole.WonderSwan => "WonderSwan",
        SourceConsole.Supervision => "Watara Supervision",
        SourceConsole.GameAndWatch => "Game & Watch",
        SourceConsole.ColecoVision => "ColecoVision",
        SourceConsole.Intellivision => "Intellivision",
        SourceConsole.Odyssey2 => "Odyssey²",
        SourceConsole.Vectrex => "Vectrex",
        _ => console.ToString(),
    };

    /// <summary>
    /// Loads (once) the white logo bitmap for a console.
    /// </summary>
    /// <param name="console">Console to show.</param>
    public static Bitmap For(SourceConsole console) => For(console.ToString());

    /// <summary>
    /// Loads (once) the white logo bitmap by file name.
    /// </summary>
    /// <param name="name">Console or company name, as the file is called.</param>
    public static Bitmap For(string name) => Load(Cache, name, UriFor(name));

    /// <summary>
    /// Resource URI of a console's white logo.
    /// </summary>
    /// <param name="console">Console to show.</param>
    public static Uri UriFor(SourceConsole console) => UriFor(console.ToString());

    /// <summary>
    /// Resource URI of a white logo by file name.
    /// </summary>
    /// <param name="name">Console or company name.</param>
    public static Uri UriFor(string name) => new(Root + name + ".png");

    /// <summary>
    /// Reads a logo into the cache on first use.
    /// </summary>
    /// <param name="cache">Cache for the variant.</param>
    /// <param name="name">File name without extension.</param>
    /// <param name="uri">Resource to read.</param>
    private static Bitmap Load(Dictionary<string, Bitmap> cache, string name, Uri uri)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        lock (cache)
        {
            if (!cache.TryGetValue(name, out var bitmap))
            {
                using var stream = AssetLoader.Open(uri);
                bitmap = new Bitmap(stream);
                cache[name] = bitmap;
            }
            return bitmap;
        }
    }
}
