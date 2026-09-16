using PD.WiiU.VirtualConsole;

namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// The UWUVCI community's per-console compatibility pages.
/// </summary>
public sealed class UwuvciCompatibilityLists : ICompatibilityLists
{
    /// <summary>
    /// Site that hosts the pages.
    /// </summary>
    public const string Root = "https://uwuvci-prime.github.io/UWUVCI-Resources/";

    private readonly ILinkOpener _links;

    /// <summary>
    /// Creates a new instance of the <see cref="UwuvciCompatibilityLists"/> class.
    /// </summary>
    /// <param name="links">Opens the page.</param>
    public UwuvciCompatibilityLists(ILinkOpener links)
    {
        _links = links ?? throw new ArgumentNullException(nameof(links));
    }

    /// <inheritdoc/>
    public Uri For(SourceConsole console)
    {
        var page = Page(console);
        return new Uri($"{Root}{page}/{page}.html");
    }

    /// <inheritdoc/>
    public Task<bool> OpenAsync(SourceConsole console) => _links.OpenAsync(For(console));

    /// <summary>
    /// Folder name the site uses for a console.
    /// </summary>
    private static string Page(SourceConsole console) => console switch
    {
        SourceConsole.Nes => "nes",
        SourceConsole.Snes => "snes",
        SourceConsole.N64 => "n64",
        SourceConsole.Gba => "gba",
        SourceConsole.Nds => "nds",
        SourceConsole.Tg16 => "tgfx",
        SourceConsole.Msx => "msx",
        SourceConsole.Wii => "wii",
        SourceConsole.GameCube => "gcn",
        _ => throw new ArgumentOutOfRangeException(nameof(console), console, "No compatibility list for this console."),
    };
}
