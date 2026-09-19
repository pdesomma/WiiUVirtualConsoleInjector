# Wii U Virtual Console Injector

Turns a ROM or disc image into an installable Wii U title, using a stock Virtual Console game as the base — or, for the Sega and Atari consoles (the ST included), the Virtual Boy, PlayStation, Arcade, the SNK tile (Neo Geo, Neo Geo CD, Neo Geo Pocket), the Other Handhelds tile (WonderSwan, Watara Supervision, Game & Watch), the Other consoles tile (ColecoVision, Intellivision, Odyssey², Vectrex), the Computers tile (DOS, Commodore 64/128, Amstrad CPC, ZX Spectrum) and Pokémon Mini, a bundled RetroArch core. A rewrite of [UWUVCI AIO](https://github.com/stuff-by-3-random-dudes/UWUVCI-AIO-WPF): same recipes, no bundled tools, no bundled keys, runs on Windows, Linux and macOS.

## Download

[Releases](https://github.com/pdesomma/WiiUVirtualConsoleInjector/releases): unzip, run. Self-contained, nothing to install.

| Build | For |
|---|---|
| `win-x64` | Windows 10 or later |
| `linux-x64` | Linux (x86-64) |
| `osx-arm64` | Apple Silicon Macs |
| `osx-x64` | Intel Macs |

## Consoles

NES, SNES, Nintendo 64, Game Boy Advance (and Game Boy / Game Boy Color through Goomba), Nintendo DS, TurboGrafx-16 (HuCard and TurboCD), MSX, Wii, GameCube (through Nintendont).

**Sega Genesis, Sega CD, Master System, Game Gear, 32X, Atari 2600, 7800, Lynx, ST, Virtual Boy, PlayStation, Pokémon Mini, the SNK tile (Neo Geo, Neo Geo CD, Neo Geo Pocket), the Other Handhelds tile (WonderSwan, Watara Supervision, Game & Watch), the Other consoles tile (ColecoVision, Intellivision, Odyssey², Vectrex), the Computers tile (DOS, Commodore 64, Commodore 128, Amstrad CPC, ZX Spectrum), Arcade, Neo Geo and Neo Geo CD** titles are different: there is no Virtual Console base, so the title carries a libretro core (Genesis Plus GX, its widescreen build, PicoDrive, Gearsystem, Stella, ProSystem, Handy, Hatari, Beetle VB, PCSX-ReARMed, NeoCD, PokeMini, Beetle NeoPop, RACE, Beetle Cygne, Potator, GW, Gearcoleco, FreeIntv, O2EM, vecx, DOSBox Pure, VICE x64, VICE x128, Caprice32, CrocoDS, Fuse, FinalBurn Neo, FB Alpha 2012 or MAME 2000/2003/2010) as its emulator with the ROM beside it. They run under **Aroma only**, read and write settings and saves under `sd:/retroarch/`, and need Aroma's signature patch module ([01_sigpatches.rpx](https://github.com/marco-calautti/SigpatchesModuleWiiU/releases) in `wiiu/environments/aroma/modules/setup/`) to install; the Review step checks the card for both. Quit from RetroArch's own menu — closing the software from the HOME Menu hangs on the Wii U Menu, a RetroArch bug.

Arcade romsets are tied to an emulator version: pick the core that matches your set (FinalBurn Neo for a current FBNeo set, FB Alpha 2012 for a 2012 set, a MAME core for the matching MAME version). A clone needs its parent zip and some sets need a BIOS zip or samples; add those on the Options step and they are copied into the title beside the game, names unchanged. Neo Geo games need `neogeo.zip` the same way.

PlayStation games go in as cue/bin, chd, pbp or an m3u playlist for multi-disc games; the bins a cue lists (and the cues an m3u lists) come along automatically. The core needs a PlayStation BIOS in `sd:/retroarch/system/` under any of its accepted names (`scph5501.bin`, `scph5500.bin`, `scph5502.bin`, `scph1001.bin` or `psxonpsp660.bin`); the Review step checks for one, and a dump you pick on the Game step is copied to the card with the title, with a dropdown for the name it is saved under.

Sega CD and Neo Geo CD games go in the same way, as cue/bin, chd or an m3u playlist, and the files a cue or m3u lists come along the same way. Sega CD needs the BIOS matching the game's region in `sd:/retroarch/system/` (`bios_CD_U.bin`, `bios_CD_E.bin` or `bios_CD_J.bin`); Neo Geo CD needs `000-lo.lo` and a system ROM such as `neocd_z.rom` under `sd:/retroarch/system/neocd/`.

DOS games go in as a zip of the game folder (or an exe / cue); Commodore, Amstrad and Spectrum take their disk, tape or snapshot images, with an m3u for multi-disk games; Atari ST needs `tos.img` in `sd:/retroarch/system/`.

## What you need

- A Wii U with custom firmware (Aroma, Tiramisu or similar) and [WUP Installer](https://github.com/Fangal-Airbag/wup-installer-gx2) or similar to install the result.
- Your Wii U common key, or an `otp.bin` dump to read it from. Entered on **Bases & Keys**; never shipped with the app.
- A title key for each base you use. Also entered on **Bases & Keys**; the base is then downloaded from Nintendo's servers. The Sega and Atari consoles, the Virtual Boy, PlayStation, Pokémon Mini, the Other Handhelds, Other consoles and Computers tiles, Arcade, Neo Geo and Neo Geo CD need no base and no title key. Lynx needs `lynxboot.img`, Atari ST `tos.img`, PlayStation a BIOS, Sega CD its region's `bios_CD_U.bin` / `bios_CD_E.bin` / `bios_CD_J.bin`, Neo Geo CD `000-lo.lo` and a system ROM such as `neocd_z.rom` (under `neocd/`), ColecoVision `colecovision.rom`, Intellivision `exec.bin` and `grom.bin`, Odyssey² `o2rom.bin` and Pokémon Mini `bios.min` in `sd:/retroarch/system/`; the Review step checks for them, and a dump you pick on the Game step is copied to the card with the title.
- For encrypted Wii ISO/WBFS images, the Wii common key. NKit images need no key.
- For GameCube, [Nintendont](https://github.com/GaryOderNichts/Nintendont) on the SD card; **Settings** puts it there and edits its `nincfg.bin`.

## How it goes

1. **Inject**: pick the console, the base (or the RetroArch core), the ROM, artwork and options, then inject. The finished title lands in the output folder and, if a card is set, under `install/` on the SD card.
2. Install it on the console with WUP Installer.

The picker lands on the base the community found works best for each console; NES and SNES ROMs are checked against the base's size before injecting, since those overwrite the base game's own ROM in place. Community artwork is looked up by the game's code, or boot screens and icons are built from a screenshot. Every inject is kept on **History** and can be reloaded with one click.

## Also on board

- **Backups**: copies a game backup (an installable package folder) onto the SD card, or unpacks a `.wud`/`.wux` dump into one first.
- **UWUVCI AIO import**: keys, title keys and downloaded bases from the previous application are picked up on **Settings**.
- Gecko cheats for Wii injects, N64 INI editor, DS screen layouts, custom bases, dark mode, an update check.

## Building

.NET 10 SDK. The app is `WiiUVirtualConsoleInjector/`; the libraries under `PD.WiiU.*` do the work and target .NET 10 and .NET Framework 4.8. Formats live in their own packages: [WiiUSharp](https://github.com/pdesomma/WiiUSharp), [WiiSharp](https://github.com/pdesomma/WiiSharp), [TargaSharp](https://github.com/pdesomma/TargaSharp).

```
dotnet build WiiUVirtualConsoleInjector/WiiUVirtualConsoleInjector.csproj
dotnet test .tests/WiiUVirtualConsoleInjector.Tests
```

Tests for every project sit under `.tests/`. A `v*` tag publishes the release builds.

## Credits

Nothing here was invented from scratch; the injection recipes, formats and tricks come from the people listed on the app's **Credits** page and in [Attribution.md](.docs/Attribution.md). The compatibility lists and community artwork are kept by the [UWUVCI-PRIME](https://github.com/UWUVCI-PRIME) community; their [Discord](https://discord.gg/mPZpqJJVmZ) is where questions go.

## Not carried over

The vWii overclock (C2W/ancast) for homebrew DOLs, the tutorial quiz, and drag-and-drop onto fields.
