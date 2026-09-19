# Cores/<console>/*_libretro.rpx

libretro cores built for Aroma, unmodified, from the community alpha `Aroma-RA-Test+Mame2010 [12-05-26].zip` (RetroArch 1.22.2, git 3f0a2c2f1e, built 2026-05-12) posted in the GBAtemp thread [RetroArch for Wii U gets early alpha Aroma CFW compatible builds](https://gbatemp.net/threads/retroarch-for-wii-u-gets-early-alpha-aroma-cfw-compatible-builds.656984/). Source is ashquarky's wut/Aroma port of RetroArch ([libretro/RetroArch PR #14925](https://github.com/libretro/RetroArch/pull/14925), branch [ashquarky/RetroArch@wiiu-wut](https://github.com/ashquarky/RetroArch/tree/wiiu-wut)). Each RPX statically links the RetroArch frontend (GPL-3.0) with one core.

| File | Bytes | SHA-256 | Core | Licence |
|---|---|---|---|---|
| `Genesis/genesis_plus_gx_libretro.rpx` | 6,729,787 | `5a5d3cf2f55e55929ef0e38e34d4458c6644b22aced458cd96c824212e42ee6e` | [Genesis Plus GX](https://github.com/libretro/Genesis-Plus-GX) | non-commercial |
| `Genesis/genesis_plus_gx_wide_libretro.rpx` | 6,717,132 | `3f6e45bc87c47a7ee979d861554ee6be3fe07e2d66e2cf733bcc9425d82e928d` | [Genesis Plus GX Wide](https://github.com/libretro/Genesis-Plus-GX-Wide) | non-commercial |
| `Genesis/picodrive_libretro.rpx` | 6,156,010 | `3a347f38be899aff68ece47357c00e06bb177f90655f616168d8b3116936ccbf` | [PicoDrive](https://github.com/libretro/picodrive) | MAME-style non-commercial |

The injector writes one of them under `code/` as the title's executable and passes the ROM through cos.xml's `argstr` (`<rpx> fs:/vol/content/<rom>`). On the console the core reads and writes its settings, saves and states under `sd:/retroarch/`, so an Aroma card with the RetroArch data folder works as usual; nothing else from the zip is shipped. Titles built this way only run under Aroma (the cores use its Mocha and rpx loader modules) and need the signature-patch module to install.
