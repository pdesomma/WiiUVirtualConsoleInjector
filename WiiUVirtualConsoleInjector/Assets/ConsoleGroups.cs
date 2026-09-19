using PD.WiiU.VirtualConsole;
using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector.Assets;

/// <summary>
/// How the console step is laid out: companies with several consoles open into a second level, the rest sit at the top.
/// </summary>
public static class ConsoleGroups
{
    /// <summary>
    /// Company name of the Atari tile.
    /// </summary>
    public const string Atari = "Atari";
    /// <summary>
    /// Name of the tile gathering the home computers.
    /// </summary>
    public const string Computers = "Computers";
    /// <summary>
    /// Company name of the Nintendo tile.
    /// </summary>
    public const string Nintendo = "Nintendo";
    /// <summary>
    /// Name of the tile gathering the remaining consoles.
    /// </summary>
    public const string Other = "Other consoles";
    /// <summary>
    /// Logo file name of the <see cref="Other"/> tile.
    /// </summary>
    public const string OtherConsolesIcon = "OtherConsoles";
    /// <summary>
    /// Name of the tile gathering the remaining handhelds.
    /// </summary>
    public const string OtherHandhelds = "Other Handhelds";
    /// <summary>
    /// Logo file name of the <see cref="OtherHandhelds"/> tile.
    /// </summary>
    public const string OtherHandheldsIcon = "OtherHandhelds";
    /// <summary>
    /// Company name of the Sega tile.
    /// </summary>
    public const string Sega = "Sega";
    /// <summary>
    /// Company name of the SNK tile.
    /// </summary>
    public const string Snk = "SNK";
    /// <summary>
    /// Logo file name of the <see cref="Snk"/> tile.
    /// </summary>
    public const string SnkIcon = "Snk";

    /// <summary>
    /// The top level, in the order shown.
    /// </summary>
    public static readonly IReadOnlyList<ConsoleTile> Top = new[]
    {
        new ConsoleTile(Nintendo, new[] { SourceConsole.Nes, SourceConsole.Snes, SourceConsole.N64, SourceConsole.Gba, SourceConsole.GameBoy, SourceConsole.Nds, SourceConsole.VirtualBoy, SourceConsole.PokemonMini, SourceConsole.GameCube, SourceConsole.Wii }),
        new ConsoleTile(Sega, new[] { SourceConsole.Genesis, SourceConsole.SegaCd, SourceConsole.MasterSystem, SourceConsole.GameGear, SourceConsole.Sega32X }),
        new ConsoleTile(Atari, new[] { SourceConsole.Atari2600, SourceConsole.Atari7800, SourceConsole.AtariLynx, SourceConsole.AtariSt }),
        new ConsoleTile(Snk, new[] { SourceConsole.NeoGeo, SourceConsole.NeoGeoCd, SourceConsole.NeoGeoPocket }, SnkIcon),
        new ConsoleTile(OtherHandhelds, new[] { SourceConsole.WonderSwan, SourceConsole.Supervision, SourceConsole.GameAndWatch }, OtherHandheldsIcon),
        new ConsoleTile(Other, new[] { SourceConsole.ColecoVision, SourceConsole.Intellivision, SourceConsole.Odyssey2, SourceConsole.Vectrex }, OtherConsolesIcon),
        new ConsoleTile(Computers, new[] { SourceConsole.Dos, SourceConsole.Commodore64, SourceConsole.Commodore128, SourceConsole.AmstradCpc, SourceConsole.ZxSpectrum }),
        new ConsoleTile(SourceConsole.Tg16),
        new ConsoleTile(SourceConsole.Msx),
        new ConsoleTile(SourceConsole.PlayStation),
        new ConsoleTile(SourceConsole.Arcade),
    };

    /// <summary>
    /// The company tile a console sits under, or null when it is a top-level tile.
    /// </summary>
    /// <param name="console">Console to find.</param>
    public static ConsoleTile? GroupOf(SourceConsole console) =>
        Top.FirstOrDefault(tile => tile.IsGroup && tile.Consoles.Contains(console));

    /// <summary>
    /// The tiles a company opens into.
    /// </summary>
    /// <param name="group">A company tile.</param>
    /// <exception cref="ArgumentException">Not a company.</exception>
    public static IReadOnlyList<ConsoleTile> Members(ConsoleTile group)
    {
        if (group is null)
            throw new ArgumentNullException(nameof(group));
        if (!group.IsGroup)
            throw new ArgumentException("Only a company opens into tiles.", nameof(group));
        return group.Consoles.Select(console => new ConsoleTile(console)).ToArray();
    }
}
