namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Every frame the artwork builder offers, in the order they are shown.
/// </summary>
public static class ArtworkTemplates
{
    private static readonly ArtworkTemplate[] Known =
    {
        new("nes", "NES", SourceConsole.Nes, ArtworkLayout.Standard, "NES.png", "NESalt1.png"),
        new("nes-alt", "NES, second icon", SourceConsole.Nes, ArtworkLayout.Standard, "NES.png", "NESalt2.png"),
        new("snes-pal", "SNES, PAL", SourceConsole.Snes, ArtworkLayout.Standard, "SNES-PAL.png", "SNESalt1.png"),
        new("snes-ntsc", "SNES, NTSC", SourceConsole.Snes, ArtworkLayout.Standard, "SNES-USA.png", "SNESalt1.png"),
        new("snes-sfc", "Super Famicom", SourceConsole.Snes, ArtworkLayout.Standard, "SFAM.png", "SNESalt2.png"),
        new("n64", "N64", SourceConsole.N64, ArtworkLayout.Standard, "N64.png", "N64alt1.png"),
        new("n64-alt", "N64, second icon", SourceConsole.N64, ArtworkLayout.Standard, "N64.png", "N64alt2.png"),
        new("gba", "Game Boy Advance", SourceConsole.Gba, ArtworkLayout.Gba, "GBA.png", "GBAalt1.png"),
        new("gba-alt", "Game Boy Advance, second icon", SourceConsole.Gba, ArtworkLayout.Gba, "GBA.png", "GBAalt2.png"),
        new("gbc", "Game Boy Color", SourceConsole.Gba, ArtworkLayout.Gbc, "GBC.png", "GBCalt1.png"),
        new("gbc-alt", "Game Boy Color, second icon", SourceConsole.Gba, ArtworkLayout.Gbc, "GBC.png", "GBCalt2.png"),
        new("gb", "Game Boy", SourceConsole.Gba, ArtworkLayout.Gbc, "newgameboy.png", "GBalt1.png"),
        new("gb-grey", "Game Boy, grey", SourceConsole.Gba, ArtworkLayout.Gbc, "Gameboy1.png", "GBalt2.png"),
        new("gb-green", "Game Boy, green", SourceConsole.Gba, ArtworkLayout.Gbc, "Gameboy2.png", "GBalt1.png"),
        new("nds", "Nintendo DS", SourceConsole.Nds, ArtworkLayout.Standard, "NDS.png", "NDSAlt1.png"),
        new("nds-alt", "Nintendo DS, second icon", SourceConsole.Nds, ArtworkLayout.Standard, "NDS.png", "NDSAlt2.png"),
        new("msx", "MSX", SourceConsole.Msx, ArtworkLayout.Standard, "MSX.png", "MSXalt1.png"),
        new("msx-alt", "MSX, second icon", SourceConsole.Msx, ArtworkLayout.Standard, "MSX.png", "MSXalt2.png"),
        new("tg16", "TurboGrafx-16", SourceConsole.Tg16, ArtworkLayout.Standard, "TG16.png", "TGFXalt1.png"),
        new("tgcd", "TurboGrafx-CD", SourceConsole.Tg16, ArtworkLayout.Standard, "TGCD.png", "TGFXalt2.png"),
        new("gcn", "GameCube", SourceConsole.GameCube, ArtworkLayout.Standard, "GCN.png", null),
        new("wii", "Wii", SourceConsole.Wii, ArtworkLayout.Wii, "WII.png", "Wii2.png"),
        new("wiiware", "WiiWare", SourceConsole.Wii, ArtworkLayout.Wii, "WIIWARE.png", "Wii2.png"),
        new("wii-homebrew", "Wii homebrew", SourceConsole.Wii, ArtworkLayout.Wii, "homebrew3.png", "Wii2.png"),
        new("wii-alt", "Wii, wide", SourceConsole.Wii, ArtworkLayout.Wii, "wii3New.png", "Wii2.png"),
        new("homebrew", "Homebrew", SourceConsole.Wii, ArtworkLayout.Standard, "homebrew.png", null),
        new("homebrew-alt", "Homebrew, second frame", SourceConsole.Wii, ArtworkLayout.Standard, "homebrew2.png", null),
        new("plain", "No frame", SourceConsole.Nes, ArtworkLayout.Standard, null, null),
    };

    /// <summary>
    /// Every template, whatever the console.
    /// </summary>
    public static IReadOnlyList<ArtworkTemplate> All => Known;

    /// <summary>
    /// Templates for one console, with the frameless one last.
    /// </summary>
    /// <param name="console">Console being injected.</param>
    public static IReadOnlyList<ArtworkTemplate> For(SourceConsole console) =>
        Known.Where(t => t.Console == console || t.Key == "plain").ToArray();

    /// <summary>
    /// The template with that key, or null.
    /// </summary>
    /// <param name="key">Key to find.</param>
    public static ArtworkTemplate? Find(string? key) => Known.FirstOrDefault(t => t.Key == key);
}
