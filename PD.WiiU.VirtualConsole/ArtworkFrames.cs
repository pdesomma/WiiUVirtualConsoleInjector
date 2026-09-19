using WiiUSharp;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Every frame the artwork builder offers, per slot, in the order they are shown.
/// </summary>
public static class ArtworkFrames
{
    /// <summary>
    /// Screenshot window on a boot screen for a 4:3 console.
    /// </summary>
    public static readonly PixelRect BootStandard = new(131, 249, 400, 300);
    /// <summary>
    /// Screenshot window on a boot screen for the Game Boy Advance.
    /// </summary>
    public static readonly PixelRect BootGba = new(132, 260, 399, 266);
    /// <summary>
    /// Screenshot window on a boot screen for the Game Boy and Game Boy Color.
    /// </summary>
    public static readonly PixelRect BootGbc = new(183, 260, 296, 266);
    /// <summary>
    /// Screenshot window on a boot screen for the Wii.
    /// </summary>
    public static readonly PixelRect BootWii = new(224, 200, 832, 333);
    /// <summary>
    /// Screenshot window on the generic Virtual Console icon.
    /// </summary>
    public static readonly PixelRect IconStandard = new(3, 9, 122, 92);
    /// <summary>
    /// The whole icon, for no overlay at all.
    /// </summary>
    public static readonly PixelRect IconFull = new(0, 0, 128, 128);
    /// <summary>
    /// The whole boot screen, for no overlay at all.
    /// </summary>
    public static readonly PixelRect BootFull = new(0, 0, 1280, 720);
    /// <summary>
    /// Screenshot window on the console icons, which carry a badge along the top.
    /// </summary>
    public static readonly PixelRect IconBadged = new(0, 23, 128, 94);
    /// <summary>
    /// Where the boot logo's text sits.
    /// </summary>
    public static readonly PixelRect LogoText = new(18, 5, 134, 32);

    private static readonly ArtworkFrame[] Known =
    {
        // Boot screens
        Boot("nes", "NES", SourceConsole.Nes, "NES.png", BootStandard),
        Boot("snes-pal", "SNES, PAL", SourceConsole.Snes, "SNES-PAL.png", BootStandard),
        Boot("snes-ntsc", "SNES, NTSC", SourceConsole.Snes, "SNES-USA.png", BootStandard),
        Boot("snes-sfc", "Super Famicom", SourceConsole.Snes, "SFAM.png", BootStandard),
        Boot("n64", "N64", SourceConsole.N64, "N64.png", BootStandard),
        Boot("gba", "Game Boy Advance", SourceConsole.Gba, "GBA.png", BootGba),
        Boot("gbc", "Game Boy Color", SourceConsole.Gba, "GBC.png", BootGbc),
        Boot("gb", "Game Boy", SourceConsole.Gba, "newgameboy.png", BootGbc),
        Boot("gb-grey", "Game Boy, grey", SourceConsole.Gba, "Gameboy1.png", BootGbc),
        Boot("gb-green", "Game Boy, green", SourceConsole.Gba, "Gameboy2.png", BootGbc),
        Boot("nds", "Nintendo DS", SourceConsole.Nds, "NDS.png", BootStandard),
        Boot("msx", "MSX", SourceConsole.Msx, "MSX.png", BootStandard),
        Boot("tg16", "TurboGrafx-16", SourceConsole.Tg16, "TG16.png", BootStandard),
        Boot("tgcd", "TurboGrafx-CD", SourceConsole.Tg16, "TGCD.png", BootStandard),
        Boot("gcn", "GameCube", SourceConsole.GameCube, "GCN.png", BootStandard),
        Boot("wii", "Wii", SourceConsole.Wii, "WII.png", BootWii),
        Boot("wiiware", "WiiWare", SourceConsole.Wii, "WIIWARE.png", BootWii),
        Boot("wii-homebrew", "Homebrew, Wii", SourceConsole.Wii, "homebrew3.png", BootWii),
        Boot("wii-narrow", "Wii, 4:3", SourceConsole.Wii, "wii3New.png", BootStandard),
        Boot("homebrew", "Homebrew", SourceConsole.Wii, "homebrew.png", BootWii),
        Boot("homebrew-2", "Homebrew, second", SourceConsole.Wii, "homebrew2.png", BootWii),
        // no Genesis art yet; the Homebrew Launcher frames match the title template
        Boot("genesis-homebrew", "Homebrew, Genesis", SourceConsole.Genesis, "homebrew.png", BootWii),
        Boot("sms-homebrew", "Homebrew, Master System", SourceConsole.MasterSystem, "homebrew.png", BootWii),
        Boot("gg-homebrew", "Homebrew, Game Gear", SourceConsole.GameGear, "homebrew.png", BootWii),
        Boot("32x-homebrew", "Homebrew, 32X", SourceConsole.Sega32X, "homebrew.png", BootWii),
        Boot("2600-homebrew", "Homebrew, Atari 2600", SourceConsole.Atari2600, "homebrew.png", BootWii),
        Boot("7800-homebrew", "Homebrew, Atari 7800", SourceConsole.Atari7800, "homebrew.png", BootWii),
        Boot("lynx-homebrew", "Homebrew, Atari Lynx", SourceConsole.AtariLynx, "homebrew.png", BootWii),
        Boot("vb-homebrew", "Homebrew, Virtual Boy", SourceConsole.VirtualBoy, "homebrew.png", BootWii),
        Boot("boot-plain", "None", null, null, BootFull),

        // Icons
        Icon("icon-vc", "Virtual Console", null, "Icon.png", IconStandard),
        Icon("icon-nes-1", "NES, style 1", SourceConsole.Nes, "NESalt1.png", IconBadged),
        Icon("icon-nes-2", "NES, style 2", SourceConsole.Nes, "NESalt2.png", IconBadged),
        Icon("icon-snes-1", "SNES, style 1", SourceConsole.Snes, "SNESalt1.png", IconBadged),
        Icon("icon-snes-2", "SNES, style 2", SourceConsole.Snes, "SNESalt2.png", IconBadged),
        Icon("icon-n64-1", "N64, style 1", SourceConsole.N64, "N64alt1.png", IconBadged),
        Icon("icon-n64-2", "N64, style 2", SourceConsole.N64, "N64alt2.png", IconBadged),
        Icon("icon-gba-1", "Game Boy Advance, style 1", SourceConsole.Gba, "GBAalt1.png", IconBadged),
        Icon("icon-gba-2", "Game Boy Advance, style 2", SourceConsole.Gba, "GBAalt2.png", IconBadged),
        Icon("icon-gbc-1", "Game Boy Color, style 1", SourceConsole.Gba, "GBCalt1.png", IconBadged),
        Icon("icon-gbc-2", "Game Boy Color, style 2", SourceConsole.Gba, "GBCalt2.png", IconBadged),
        Icon("icon-gb-1", "Game Boy, style 1", SourceConsole.Gba, "GBalt1.png", IconBadged),
        Icon("icon-gb-2", "Game Boy, style 2", SourceConsole.Gba, "GBalt2.png", IconBadged),
        Icon("icon-nds-1", "Nintendo DS, style 1", SourceConsole.Nds, "NDSAlt1.png", IconBadged),
        Icon("icon-nds-2", "Nintendo DS, style 2", SourceConsole.Nds, "NDSAlt2.png", IconBadged),
        Icon("icon-msx-1", "MSX, style 1", SourceConsole.Msx, "MSXalt1.png", IconBadged),
        Icon("icon-msx-2", "MSX, style 2", SourceConsole.Msx, "MSXalt2.png", IconBadged),
        Icon("icon-tg16-1", "TurboGrafx, style 1", SourceConsole.Tg16, "TGFXalt1.png", IconBadged),
        Icon("icon-tg16-2", "TurboGrafx, style 2", SourceConsole.Tg16, "TGFXalt2.png", IconBadged),
        Icon("icon-gcn-1", "GameCube, style 1", SourceConsole.GameCube, "GCNICON2.png", IconBadged),
        Icon("icon-gcn-2", "GameCube, style 2", SourceConsole.GameCube, "GCNICON3.png", IconBadged),
        Icon("icon-wii", "Wii", SourceConsole.Wii, "Wii2.png", IconBadged),
        Icon("icon-wii-2", "Wii, style 2", SourceConsole.Wii, "WiiIcon.png", IconBadged),
        Icon("icon-homebrew", "Homebrew", SourceConsole.Wii, "HBICON.png", IconBadged),
        Icon("icon-genesis-homebrew", "Homebrew, Genesis", SourceConsole.Genesis, "HBICON.png", IconBadged),
        Icon("icon-sms-homebrew", "Homebrew, Master System", SourceConsole.MasterSystem, "HBICON.png", IconBadged),
        Icon("icon-gg-homebrew", "Homebrew, Game Gear", SourceConsole.GameGear, "HBICON.png", IconBadged),
        Icon("icon-32x-homebrew", "Homebrew, 32X", SourceConsole.Sega32X, "HBICON.png", IconBadged),
        Icon("icon-2600-homebrew", "Homebrew, Atari 2600", SourceConsole.Atari2600, "HBICON.png", IconBadged),
        Icon("icon-7800-homebrew", "Homebrew, Atari 7800", SourceConsole.Atari7800, "HBICON.png", IconBadged),
        Icon("icon-lynx-homebrew", "Homebrew, Atari Lynx", SourceConsole.AtariLynx, "HBICON.png", IconBadged),
        Icon("icon-vb-homebrew", "Homebrew, Virtual Boy", SourceConsole.VirtualBoy, "HBICON.png", IconBadged),
        Icon("icon-plain", "None", null, null, IconFull),

        // Boot logos
        new("logo-pill", "Pill", ImageSlot.BootLogo, null, "bootLogoTex.png", null),
        new("logo-plain", "None", ImageSlot.BootLogo, null, null, null),
    };

    /// <summary>
    /// Every frame, whatever the slot or console.
    /// </summary>
    public static IReadOnlyList<ArtworkFrame> All => Known;

    /// <summary>
    /// The frame with that key, or null.
    /// </summary>
    /// <param name="key">Key to find.</param>
    public static ArtworkFrame? Find(string? key) => Known.FirstOrDefault(f => f.Key == key);

    /// <summary>
    /// Frames for one slot and console, the console's own first and the plain one last; the GamePad screen shares the TV frames.
    /// </summary>
    /// <param name="slot">Slot being drawn.</param>
    /// <param name="console">Console being injected.</param>
    public static IReadOnlyList<ArtworkFrame> For(ImageSlot slot, SourceConsole console)
    {
        if (slot is null)
            throw new ArgumentNullException(nameof(slot));

        var artSlot = slot == ImageSlot.BootDrc ? ImageSlot.BootTv : slot;
        return Known
            .Where(f => f.Slot == artSlot && (f.Console == console || f.Console is null))
            .OrderBy(f => f.IsPlain ? 2 : f.Console is null ? 1 : 0)
            .ToArray();
    }

    /// <summary>
    /// The screenshot window with no overlay: the whole image.
    /// </summary>
    /// <param name="slot">Slot being drawn.</param>
    public static PixelRect? DefaultWindow(ImageSlot slot) =>
        slot == ImageSlot.Icon ? IconFull : slot == ImageSlot.BootLogo ? null : BootFull;

    private static ArtworkFrame Boot(string key, string name, SourceConsole? console, string? resource, PixelRect window) =>
        new(key, name, ImageSlot.BootTv, console, resource, window);

    private static ArtworkFrame Icon(string key, string name, SourceConsole? console, string? resource, PixelRect window) =>
        new(key, name, ImageSlot.Icon, console, resource, window);
}
