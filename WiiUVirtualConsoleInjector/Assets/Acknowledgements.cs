using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector.Assets;

/// <summary>
/// Everyone credited on the Acknowledgements page.
/// </summary>
public static class Acknowledgements
{
    /// <summary>
    /// Projects whose logic was studied and reimplemented here. Tip links are the authors' own Ko-fi pages, checked September 2026.
    /// </summary>
    public static readonly IReadOnlyList<Acknowledgement> Borrowed = new Acknowledgement[]
    {
        new("UWUVCI AIO", "NicoAICP, Morilli, ZestyTS", "The application this one is rewritten from: its workflow, base catalogue and every injection recipe.", new Uri("https://github.com/stuff-by-3-random-dudes/UWUVCI-AIO-WPF")) { Donate = new Uri("https://ko-fi.com/uwuvci") },
        new("CNUS_Packer", "NicoAICP, Morilli", "Title packing: TMD, ticket, FST layout and content encryption.", new Uri("https://github.com/Morilli/CNUS_Packer")) { Donate = new Uri("https://ko-fi.com/nicoaicp") },
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
        new("DarkFilter Removal N64", "MelonSpeedruns, ZestyTS", "Editing FrameLayout.arc to drop the N64 dark filter.") { Donate = new Uri("https://ko-fi.com/zestyts") },
        new("wav2btsnd", "its original author", "Boot sound conversion."),
        new("png2tga / tga_verify", "Easy2Convert, Morilli", "The icon and boot image formats the console expects."),
        new("Icon and TV boot images", "Flump, ZestyTS", "The per-console artwork.") { Donate = new Uri("https://ko-fi.com/zestyts") },
        new("Image Creation Base", "Phacox", "The template approach to generating icons and boot images."),
    };

    /// <summary>
    /// Binaries and fonts shipped inside the app, unmodified.
    /// </summary>
    public static readonly IReadOnlyList<Acknowledgement> Shipped = new Acknowledgement[]
    {
        new("Goomba Color", "Dwedit, built on Goomba by FluBBa", "Game Boy and Game Boy Color emulator prepended to ROMs for the GBA Virtual Console.", new Uri("https://www.dwedit.org/gba/goombacolor.php"), "GPL"),
        new("DS layout screens", "MikaDubbz", "Extra TV and GamePad screen layouts for DS Virtual Console titles, offered on the DS options step.", new Uri("https://gbatemp.net/threads/add-many-more-screen-layout-options-in-ds-virtual-console-games.574254/"), "Community art"),
        new("Gecko code handler", "Nuke, brkirch and the USB Loader GX team", "Runs Gecko cheat codes inside an injected Wii game; baked into main.dol next to the code list.", new Uri("https://github.com/wiidev/usbloadergx"), "GPL") { Donate = new Uri("https://ko-fi.com/blackb0x") },
        new("Nintendont", "FIX94, GaryOderNichts and the Nintendont contributors", "The GameCube loader itself; Settings downloads the Wii U GamePad build onto the SD card and edits its nincfg.bin.", new Uri("https://github.com/GaryOderNichts/Nintendont"), "GPL") { Donate = new Uri("https://ko-fi.com/garyodernichts") },
        new("Nintendont autoboot forwarder", "FIX94", "main.dol of the carrier disc for GameCube injects; loads Nintendont from the SD card.", new Uri("https://github.com/FIX94/nintendont-autoboot-forwarder"), "MIT"),
        new("wiivc_chan_booter", "FIX94", "main.dol of the carrier disc for Wii channel forwarders.", new Uri("https://github.com/FIX94/wiivc_chan_booter"), "MIT"),
        new("RetroArch for Aroma", "The RetroArch team; Aroma port by ashquarky", "The libretro frontend inside every bundled core; each Sega Genesis title is one of its Aroma core builds with the ROM beside it.", new Uri("https://github.com/libretro/RetroArch/pull/14925"), "GPL"),
        new("Genesis Plus GX", "Charles MacDonald, Eke-Eke and the libretro contributors", "The emulator behind the Genesis Plus GX and Genesis Plus GX Wide cores; also serves Master System and Game Gear.", new Uri("https://github.com/libretro/Genesis-Plus-GX"), "Non-commercial"),
        new("PicoDrive", "notaz, Grazvydas Ignotas and the libretro contributors", "The lighter Sega Genesis emulator offered as a core, and the 32X one.", new Uri("https://github.com/libretro/picodrive"), "MAME-style non-commercial"),
        new("Gearsystem", "Ignacio Sánchez (drhelius)", "The lighter Master System and Game Gear emulator offered as a core.", new Uri("https://github.com/drhelius/Gearsystem"), "GPL"),
        new("Stella", "Bradford W. Mott, Stephen Anthony and the Stella team", "The Atari 2600 emulator behind the Stella 2023 core.", new Uri("https://github.com/libretro/stella2023-libretro"), "GPL"),
        new("ProSystem", "Greg Stanton and the libretro contributors", "The Atari 7800 emulator behind the ProSystem core.", new Uri("https://github.com/libretro/prosystem-libretro"), "GPL"),
        new("Handy", "Keith Wilkins and the libretro contributors", "The Atari Lynx emulator behind the Handy core.", new Uri("https://github.com/libretro/libretro-handy"), "zlib"),
        new("Beetle VB", "Mednafen team and the libretro contributors", "The Virtual Boy emulator behind the Beetle VB core.", new Uri("https://github.com/libretro/beetle-vb-libretro"), "GPL"),
        new("PCSX-ReARMed", "notaz and the libretro contributors", "The PlayStation emulator behind the PCSX-ReARMed core.", new Uri("https://github.com/libretro/pcsx_rearmed"), "GPL"),
        new("FinalBurn Neo", "FBNeo team", "The arcade and Neo Geo emulator behind the FinalBurn Neo core.", new Uri("https://github.com/libretro/FBNeo"), "FBNeo licence, non-commercial"),
        new("FB Alpha 2012", "FB Alpha team", "The 2012 FB Alpha emulator behind the FB Alpha 2012, CPS-1, CPS-2, CPS-3 and Neo Geo cores.", new Uri("https://github.com/libretro/fbalpha2012"), "non-commercial"),
        new("MAME", "MAMEdev and the libretro contributors", "The arcade emulator behind the MAME 2000, 2003 Midway, 2003-Plus and 2010 cores.", new Uri("https://github.com/libretro/mame2003-plus-libretro"), "MAME licence, GPL-2.0 and BSD-3"),
        new("Homebrew Launcher channel", "dimok789; art by cathor and Maschell", "The title skeleton (cos.xml, app.xml, meta.xml and stock boot art) RetroArch cores are packed into.", new Uri("https://github.com/dimok789/homebrew_launcher"), "GPL"),
        new("Nunito", "The Nunito Project Authors", "The interface font.", new Uri("https://github.com/googlefonts/nunito"), "SIL Open Font License 1.1"),
    };
}
