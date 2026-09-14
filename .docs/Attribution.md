# Attribution

Third-party research this code depends on. None of the code is copied; the facts are.

| Where | What | From |
|---|---|---|
| `PD.WiiU.VirtualConsole.Wii/FirmwarePatcher.cs` | The fw.img (IOS r590) byte patterns and replacements for fakesign, L/R→ZL/ZR, Wii Remote emulation, horizontal remap, AHBPROT/MEMPROT, Nintendont hooks, Wii Remote passthrough and Classic Controller reporting | [nfs2iso2nfs](https://github.com/FIX94/nfs2iso2nfs) `DoThePatching` by piratesephiroth and FIX94 (no license stated) |
| `PD.WiiU.VirtualConsole.Wii/WiiPartitionCipher.cs` | NFS payload ↔ ISO conversion flow | [nfs2iso2nfs](https://github.com/FIX94/nfs2iso2nfs) `manipulateISO` by sabykos; see WiiUSharp and WiiSharp for the format work |
| `UWUVCI AIO WPF/` | The previous application this repo forked from and is replacing; GPL-3.0, which this repo inherits | UWUVCI by NicoAICP, CasuallyCalm, ZestyTS and contributors ([stuff-by-3-random-dudes](https://github.com/stuff-by-3-random-dudes)) |
