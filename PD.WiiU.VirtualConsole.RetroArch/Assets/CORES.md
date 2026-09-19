# Cores/<console>/*_libretro.rpx

Folders group the cores by the console they were first added for; a core that serves several consoles (Genesis Plus GX also runs Master System and Game Gear, PicoDrive also runs 32X, FinalBurn Neo serves both Arcade and Neo Geo) is embedded once under its own name.

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
| `Arcade/fbneo_libretro.rpx` | 29,751,521 | `01f7c78bbc37501f1bdec0c29dbf0cd4e9165e15487754041481b98d9739104a` | [FinalBurn Neo](https://github.com/libretro/FBNeo) | FBNeo non-commercial |
| `Arcade/mame2003_plus_libretro.rpx` | 18,012,178 | `e498bdd9e7453e6418c8da52a9daf9310227f5d479009ba813747b8a0708e2d2` | [MAME 2003-Plus](https://github.com/libretro/mame2003-plus-libretro) | MAME non-commercial |
| `Arcade/mame2010_libretro.rpx` | 24,833,358 | `c4d20e5edb08084186b94935b9df5434a832450fe5043e8d6d84980bbe2bab89` | [MAME 2010](https://github.com/libretro/mame2010-libretro) | MAME non-commercial |
| `Arcade/mame2000_libretro.rpx` | 10,261,117 | `2028a46bfc53cd1d0f3c06bfab46fdd2aac09cf46e19fa3bd4f50f801a86b0c8` | [MAME 2000](https://github.com/libretro/mame2000-libretro) | MAME non-commercial |
| `Arcade/mame2003_midway_libretro.rpx` | 6,345,351 | `8a30d5600d6293e6dcf1bd2997938d18148e185e9a05c5f61f5b2dc5dbebebd0` | [MAME 2003 Midway](https://github.com/libretro/mame2003_midway) | MAME non-commercial |
| `Arcade/fbalpha2012_libretro.rpx` | 12,335,794 | `187b6de8bf5ef34793b201a357cddf94203e33f0d3f0b66aa81a91bae5d5fced` | [FB Alpha 2012](https://github.com/libretro/fbalpha2012) | non-commercial |
| `Arcade/fbalpha2012_cps1_libretro.rpx` | 6,054,951 | `983db0f299ba1b3fd53f57c071793da869ac3929deaa7b0284cb8ba685355b54` | [FB Alpha 2012 CPS-1](https://github.com/libretro/fbalpha2012_cps1) | non-commercial |
| `Arcade/fbalpha2012_cps2_libretro.rpx` | 5,944,447 | `b25738a5437d5f73b1e685fe2b5c269d2c3c8dbcf15d3525fe6917b25cb316ca` | [FB Alpha 2012 CPS-2](https://github.com/libretro/fbalpha2012_cps2) | non-commercial |
| `Arcade/fbalpha2012_cps3_libretro.rpx` | 5,506,159 | `c21533a57deb828c5a03a4f0d2b08c5ef3c99e57f1a29c5cd3b222027b872639` | [FB Alpha 2012 CPS-3](https://github.com/libretro/fbalpha2012_cps3) | non-commercial |
| `Arcade/fbalpha2012_neogeo_libretro.rpx` | 6,099,616 | `b22e510c48ee2bd42c2b2689bbb8d5f3c0a478dfe4487afd235bb8e236ac9d79` | [FB Alpha 2012 Neo Geo](https://github.com/libretro/fbalpha2012_neogeo) | non-commercial |

The injector writes one of them under `code/` as the title's executable and passes the ROM through cos.xml's `argstr` (`<rpx> fs:/vol/content/<rom>`). On the console the core reads and writes its settings, saves and states under `sd:/retroarch/`, so an Aroma card with the RetroArch data folder works as usual; nothing else from the zip is shipped. Titles built this way only run under Aroma (the cores use its Mocha and rpx loader modules) and need the signature-patch module to install.
