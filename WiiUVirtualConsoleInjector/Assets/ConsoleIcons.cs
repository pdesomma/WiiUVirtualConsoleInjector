using Avalonia.Media.Imaging;
using Avalonia.Platform;
using PD.WiiU.VirtualConsole;

namespace WiiUVirtualConsoleInjector.Assets;

/// <summary>
/// The white console logos shipped as resources, one per <see cref="SourceConsole"/>.
/// </summary>
public static class ConsoleIcons
{
    private const string Root = "avares://WiiUVirtualConsoleInjector/Assets/Consoles/";

    private static readonly Dictionary<SourceConsole, Bitmap> Cache = new();

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
        _ => console.ToString(),
    };

    /// <summary>
    /// Loads (once) the logo bitmap for a console.
    /// </summary>
    /// <param name="console">Console to show.</param>
    public static Bitmap For(SourceConsole console)
    {
        lock (Cache)
        {
            if (!Cache.TryGetValue(console, out var bitmap))
            {
                using var stream = AssetLoader.Open(UriFor(console));
                bitmap = new Bitmap(stream);
                Cache[console] = bitmap;
            }
            return bitmap;
        }
    }

    /// <summary>
    /// Resource URI of a console's logo.
    /// </summary>
    /// <param name="console">Console to show.</param>
    public static Uri UriFor(SourceConsole console) => new(Root + console + ".png");
}
