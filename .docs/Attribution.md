# Attribution

Third-party research this code depends on. None of the code is copied; the facts are.

| Where | What | From |
|---|---|---|
| `PD.WiiU.VirtualConsole.Wii/FirmwarePatcher.cs` | The fw.img (IOS r590) byte patterns and replacements for fakesign, L/R→ZL/ZR, Wii Remote emulation, horizontal remap, AHBPROT/MEMPROT, Nintendont hooks, Wii Remote passthrough and Classic Controller reporting | [nfs2iso2nfs](https://github.com/FIX94/nfs2iso2nfs) `DoThePatching` by piratesephiroth and FIX94 (no license stated) |
| `PD.WiiU.VirtualConsole.Wii/WiiPartitionCipher.cs` | NFS payload ↔ ISO conversion flow | [nfs2iso2nfs](https://github.com/FIX94/nfs2iso2nfs) `manipulateISO` by sabykos; see WiiUSharp and WiiSharp for the format work |
| `PD.WiiU.VirtualConsole.Retro/RomSlot.cs` | Where the NES/SNES ROM sits in the Virtual Console executable: the WUP- marker, the size-key byte and its capacity table, the sixteen-byte NES header allowance | [Retroinject_C](https://github.com/Morilli/Retroinject_C) by Morilli and moonshadow565 (no license stated) |
| `PD.WiiU.VirtualConsole.Retro/AspectRatioPatch.cs` | The TV and GamePad display-size instruction patterns and their 8:7 / 16:9 replacements | [ChangeAspectRatio](https://github.com/andot/ChangeAspectRatio) by andot, MIT |
| `PD.WiiU.VirtualConsole.Retro/N64Rom.cs` | The three N64 ROM byte orders and their headers | [N64Converter_C](https://github.com/Morilli/N64Converter_C) by Morilli (no license stated) |
| `PD.WiiU.VirtualConsole.Retro/FrameLayoutPatch.cs` | The SARC/FLYT offsets of the frame and frame_mask panes and the values that make the frame full-height, 4:3 or 16:9, and hide the dark mask | `N64FrameLayoutPatcher` in the previous application (below) |
| `UWUVCI AIO WPF/` | The previous application this repo forked from and is replacing; GPL-3.0, which this repo inherits | UWUVCI by NicoAICP, CasuallyCalm, ZestyTS and contributors ([stuff-by-3-random-dudes](https://github.com/stuff-by-3-random-dudes)) |
