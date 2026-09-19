# Cores/<console>/*_libretro.rpx

Folders group the cores by the console they were first added for; a core that serves several consoles (Genesis Plus GX also runs Master System and Game Gear, PicoDrive also runs 32X) is embedded once under its own name.

libretro cores built for Aroma, unmodified, from the community alpha `Aroma-RA-Test+Mame2010 [12-05-26].zip` (RetroArch 1.22.2, git 3f0a2c2f1e, built 2026-05-12) posted in the GBAtemp thread [RetroArch for Wii U gets early alpha Aroma CFW compatible builds](https://gbatemp.net/threads/retroarch-for-wii-u-gets-early-alpha-aroma-cfw-compatible-builds.656984/). Source is ashquarky's wut/Aroma port of RetroArch ([libretro/RetroArch PR #14925](https://github.com/libretro/RetroArch/pull/14925), branch [ashquarky/RetroArch@wiiu-wut](https://github.com/ashquarky/RetroArch/tree/wiiu-wut)). Each RPX statically links the RetroArch frontend (GPL-3.0) with one core.

| File | Bytes | SHA-256 | Core | Licence |
|---|---|---|---|---|
| `Genesis/genesis_plus_gx_libretro.rpx` | 6,729,787 | `5a5d3cf2f55e55929ef0e38e34d4458c6644b22aced458cd96c824212e42ee6e` | [Genesis Plus GX](https://github.com/libretro/Genesis-Plus-GX) | non-commercial |
| `Genesis/genesis_plus_gx_wide_libretro.rpx` | 6,717,132 | `3f6e45bc87c47a7ee979d861554ee6be3fe07e2d66e2cf733bcc9425d82e928d` | [Genesis Plus GX Wide](https://github.com/libretro/Genesis-Plus-GX-Wide) | non-commercial |
| `Genesis/picodrive_libretro.rpx` | 6,156,010 | `3a347f38be899aff68ece47357c00e06bb177f90655f616168d8b3116936ccbf` | [PicoDrive](https://github.com/libretro/picodrive) | MAME-style non-commercial |
| `MasterSystem/gearsystem_libretro.rpx` | 5,841,357 | `aa7ff386f68cfbe6e0dd291f845b32887e4ea0541cc5586e15d7a1906283ab4d` | [Gearsystem](https://github.com/drhelius/Gearsystem) | GPL-3.0 |
| `Atari2600/stella2023_libretro.rpx` | 7,344,333 | `8c533850d2b3cb08d26491fe8e1b06b317d2bef938913ced978f8f9d17525c10` | [Stella 2023](https://github.com/libretro/stella2023-libretro) | GPL-2.0 |
| `Atari7800/prosystem_libretro.rpx` | 5,471,817 | `aad44794b8259cc3e26bd315f598d6a1cda7b43fa59dedfb8b05e30c5a3cf8dd` | [ProSystem](https://github.com/libretro/prosystem-libretro) | GPL-2.0 |
| `AtariLynx/handy_libretro.rpx` | 5,516,495 | `9235a207810f224618efc01d5200fe12428d55fb40b7767b1cbd41e0d76d2b0b` | [Handy](https://github.com/libretro/libretro-handy) | zlib |
| `VirtualBoy/mednafen_vb_libretro.rpx` | 5,494,527 | `5b98b8163f68a2df4cdaa1d7745a04816f477f77d1d98556d4bf22078fa47ae6` | [Beetle VB](https://github.com/libretro/beetle-vb-libretro) | GPL-2.0 |

The injector writes one of them under `code/` as the title's executable and passes the ROM through cos.xml's `argstr` (`<rpx> fs:/vol/content/<rom>`). On the console the core reads and writes its settings, saves and states under `sd:/retroarch/`, so an Aroma card with the RetroArch data folder works as usual; nothing else from the zip is shipped. Titles built this way only run under Aroma (the cores use its Mocha and rpx loader modules) and need the signature-patch module to install.
