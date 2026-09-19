# Cores/<console>/*_libretro.rpx

Folders group the cores by the console they were first added for; a core that serves several consoles (Genesis Plus GX also runs Master System, Game Gear and Sega CD, PicoDrive also runs 32X and Sega CD, FinalBurn Neo serves both Arcade and Neo Geo) is embedded once under its own name.

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
| `PlayStation/pcsx_rearmed_libretro.rpx` | 6,231,138 | `9ed67de8151116629a3b99615e19eaf4a44e0c297ab2b2ebbc4041f27b1863be` | [PCSX-ReARMed](https://github.com/libretro/pcsx_rearmed) | GPL-2.0 |
| `PokemonMini/pokemini_libretro.rpx` | 5,545,354 | `0139c2928b76b329711bff1363cf3afb74018e05cc3d289d232313dfb8d7593e` | [PokeMini](https://github.com/libretro/PokeMini) | GPL-3.0 |
| `NeoGeoPocket/mednafen_ngp_libretro.rpx` | 5,582,405 | `cb788edb0b65eaef09dc355f01b52280a42dc5af903302b9289e8491fc3678e3` | [Beetle NeoPop](https://github.com/libretro/beetle-ngp-libretro) | GPL-2.0 |
| `NeoGeoPocket/race_libretro.rpx` | 5,551,384 | `74abbdd09ded120d03ed64a3ba11f65712ac5c57838c2d6288c46faea1ea68db` | [RACE](https://github.com/libretro/RACE) | GPL-2.0 |
| `WonderSwan/mednafen_wswan_libretro.rpx` | 5,593,151 | `ead3bf4ff32b0053d67a7ae1b38fffc7ed8567c02cd87ef46904f164ce098250` | [Beetle WonderSwan](https://github.com/libretro/beetle-wswan-libretro) | GPL-2.0 |
| `Supervision/potator_libretro.rpx` | 5,437,345 | `029988434190fa151048b6528c78abe413c5b4817d66b3356c80c21ff3eb55b1` | [Potator](https://github.com/libretro/potator) | MIT |
| `GameAndWatch/gw_libretro.rpx` | 5,643,936 | `c1ba0142adcc323cd605d72dacb21f4e74c22b39a14f479274942a48f2641195` | [GW](https://github.com/libretro/gw-libretro) | GPL-3.0 |
| `ColecoVision/gearcoleco_libretro.rpx` | 5,823,933 | `e977864a40b17353d9382c15b185a93d9899dd259e646c191b0d67b8325e9d18` | [Gearcoleco](https://github.com/drhelius/Gearcoleco) | GPL-3.0 |
| `Intellivision/freeintv_libretro.rpx` | 5,451,978 | `861c1fc1973f9cce8c9e79b6d008172776efd913bc33064f0344e7ee0ae0a183` | [FreeIntv](https://github.com/libretro/FreeIntv) | GPL-3.0 |
| `Odyssey2/o2em_libretro.rpx` | 5,520,131 | `3caaf56cacb45b89b70cf65f77d228732c4a0175e036e541a03d6c2c5b97c251` | [O2EM](https://github.com/libretro/libretro-o2em) | Artistic License 2.0 |
| `Vectrex/vecx_libretro.rpx` | 5,467,604 | `f57b1b54fe4525a731fab8815e468fd935f2ac9e780174098edc84a33370e241` | [vecx](https://github.com/libretro/libretro-vecx) | GPL-3.0 |
| `NeoGeoCd/neocd_libretro.rpx` | 6,419,656 | `4f46f914205425ad3fe5ba0f023c4419905a13af12db4fccf521a1c9aca704a6` | [NeoCD](https://github.com/libretro/neocd_libretro) | LGPL-3.0 |
| `Dos/dosbox_pure_libretro.rpx` | 7,414,602 | `79ad634d120621fc1f26b18e5f274cdec0ec23fd02d40d9e8471bd309beebbc3` | [DOSBox Pure](https://github.com/schellingb/dosbox-pure) | GPL-2.0 |
| `Commodore64/vice_x64_libretro.rpx` | 7,577,989 | `67ff5ceb49066cd312d59ce2b258350273b510c774d87554da85d2facc6c9f80` | [VICE x64](https://github.com/libretro/vice-libretro) | GPL-2.0 |
| `Commodore128/vice_x128_libretro.rpx` | 7,903,418 | `9b7c683ef241041bd1dd4f924ba66d0af75dca4365922774cfe5d650a0993132` | [VICE x128](https://github.com/libretro/vice-libretro) | GPL-2.0 |
| `AmstradCpc/cap32_libretro.rpx` | 5,806,774 | `938186419b8b7d5b96a3e06a66749a42d5cf13a1b10d93699d3c2f7df16efc00` | [Caprice32](https://github.com/libretro/libretro-cap32) | GPL-2.0 |
| `AmstradCpc/crocods_libretro.rpx` | 5,694,943 | `864566c2b065915687d0a8ebdb4968e2f9f5a7b83ac1bcd37c4faaa7b5e065e9` | [CrocoDS](https://github.com/libretro/libretro-crocods) | MIT |
| `ZxSpectrum/fuse_libretro.rpx` | 6,533,579 | `194c4ff9c2a83bd05b1859c7ea4334d3cdbf6b1ccbbc0eeaae6ec8a210087dc1` | [Fuse](https://github.com/libretro/fuse-libretro) | GPL-3.0 |
| `AtariSt/hatari_libretro.rpx` | 7,079,134 | `c1549c867f87b5df3c1e325c3e21a42a4c386669d96bf619aae112991fc71755` | [Hatari](https://github.com/libretro/hatari) | GPL-2.0 |

The injector writes one of them under `code/` as the title's executable and passes the ROM through cos.xml's `argstr` (`<rpx> fs:/vol/content/<rom>`). On the console the core reads and writes its settings, saves and states under `sd:/retroarch/`, so an Aroma card with the RetroArch data folder works as usual; nothing else from the zip is shipped. Titles built this way only run under Aroma (the cores use its Mocha and rpx loader modules) and need the signature-patch module to install.
