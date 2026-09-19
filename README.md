# Wii U Virtual Console Injector

Turns a ROM or disc image into an installable Wii U title, using a stock Virtual Console game as the base — or, for the Sega consoles, a bundled RetroArch core. A rewrite of [UWUVCI AIO](https://github.com/stuff-by-3-random-dudes/UWUVCI-AIO-WPF): same recipes, no bundled tools, no bundled keys, runs on Windows, Linux and macOS.

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

**Sega Genesis, Master System, Game Gear and 32X** titles are different: there is no Virtual Console base, so the title carries a libretro core (Genesis Plus GX, its widescreen build, PicoDrive or Gearsystem) as its emulator with the ROM beside it. They run under **Aroma only**, read and write settings and saves under `sd:/retroarch/`, and need Aroma's signature patch module ([01_sigpatches.rpx](https://github.com/marco-calautti/SigpatchesModuleWiiU/releases) in `wiiu/environments/aroma/modules/setup/`) to install; the Review step checks the card for both. Quit from RetroArch's own menu — closing the software from the HOME Menu hangs on the Wii U Menu, a RetroArch bug.

## What you need

- A Wii U with custom firmware (Aroma, Tiramisu or similar) and [WUP Installer](https://github.com/Fangal-Airbag/wup-installer-gx2) or similar to install the result.
- Your Wii U common key, or an `otp.bin` dump to read it from. Entered on **Bases & Keys**; never shipped with the app.
- A title key for each base you use. Also entered on **Bases & Keys**; the base is then downloaded from Nintendo's servers. The Sega consoles need no base and no title key.
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
