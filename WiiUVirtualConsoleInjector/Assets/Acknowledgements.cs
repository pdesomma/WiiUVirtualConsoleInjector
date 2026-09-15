using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector.Assets;

/// <summary>
/// Everyone credited on the Acknowledgements page.
/// </summary>
public static class Acknowledgements
{
    /// <summary>
    /// Projects whose logic was studied and reimplemented here.
    /// </summary>
    public static readonly IReadOnlyList<Acknowledgement> Borrowed = new Acknowledgement[]
    {
        new("UWUVCI AIO", "NicoAICP, Morilli, ZestyTS", "The application this one is rewritten from: its workflow, base catalogue and every injection recipe.", new Uri("https://github.com/stuff-by-3-random-dudes/UWUVCI-AIO-WPF")),
        new("CNUS_Packer", "NicoAICP, Morilli", "Title packing: TMD, ticket, FST layout and content encryption.", new Uri("https://github.com/Morilli/CNUS_Packer")),
        new("Cdecrypt", "crediar", "Title decryption and the hash tree checks.", new Uri("https://github.com/VitaSmith/cdecrypt")),
        new("WiiUDownloader", "Morilli", "Fetching bases from the update servers.", new Uri("https://github.com/Morilli/WiiUDownloader")),
        new("RetroInject_C", "Morilli", "NES and SNES ROM replacement inside the emulator executable.", new Uri("https://github.com/Morilli/RetroInject_C")),
        new("inject_gba_c", "Morilli", "The Game Boy Advance PSB archive format and ROM injection.", new Uri("https://github.com/Morilli/inject_gba_c")),
        new("N64Converter", "Morilli", "Byte-swapping N64 ROMs to the native order."),
        new("BuildPcePkg / BuildTurboCDPcePkg", "JohnnyGo", "The pce.pkg archive the TurboGrafx-16 emulator loads."),
        new("wiiurpxtool", "0CHB0", "RPX/RPL compression and decompression.", new Uri("https://github.com/0CBH0/wiiurpxtool")),
        new("wit", "Wiimm", "Wii disc structure, partition rebuilding and hashing.", new Uri("https://wit.wiimm.de/")),
        new("nfs2iso2nfs", "sabykos, piratesephiroth, FIX94 and many more", "The NFS container the Wii U's Wii mode reads discs from, and the fw.img patches.", new Uri("https://github.com/sabykos/nfs2iso2nfs")),
        new("GetExtTypePatcher", "FIX94", "Making the vWii firmware accept homebrew discs.", new Uri("https://github.com/FIX94/GetExtTypePatcher")),
        new("Wii-VMC", "wanikoko", "Rewriting main.dol video modes to another TV standard."),
        new("WiiGameLanguage Patcher", "ReturnerS", "Region and language patching of Wii disc images."),
        new("ChangeAspectRatio", "andot", "The NES/SNES display-size instruction patches."),
        new("DarkFilter Removal N64", "MelonSpeedruns, ZestyTS", "Editing FrameLayout.arc to drop the N64 dark filter."),
        new("wav2btsnd", "its original author", "Boot sound conversion."),
        new("png2tga / tga_verify", "Easy2Convert, Morilli", "The icon and boot image formats the console expects."),
        new("Icon and TV boot images", "Flump, ZestyTS", "The per-console artwork."),
        new("Image Creation Base", "Phacox", "The template approach to generating icons and boot images."),
    };

    /// <summary>
    /// Binaries and fonts shipped inside the app, unmodified.
    /// </summary>
    public static readonly IReadOnlyList<Acknowledgement> Shipped = new Acknowledgement[]
    {
        new("Goomba Color", "Dwedit, built on Goomba by FluBBa", "Game Boy and Game Boy Color emulator prepended to ROMs for the GBA Virtual Console.", new Uri("https://www.dwedit.org/gba/goombacolor.php"), "GPL"),
        new("Nintendont autoboot forwarder", "FIX94", "main.dol of the carrier disc for GameCube injects; loads Nintendont from the SD card.", new Uri("https://github.com/FIX94/nintendont-autoboot-forwarder"), "MIT"),
        new("wiivc_chan_booter", "FIX94", "main.dol of the carrier disc for Wii channel forwarders.", new Uri("https://github.com/FIX94/wiivc_chan_booter"), "MIT"),
        new("Nunito", "The Nunito Project Authors", "The interface font.", new Uri("https://github.com/googlefonts/nunito"), "SIL Open Font License 1.1"),
    };
}
