# codehandleronly.bin

The Gecko OS cheat code handler (Nuke, brkirch and contributors), the build without the USB Gecko debugger, as shipped in USB Loader GX (`source/patches/codehandleronly.h`, converted back from the C array, unmodified). 2,736 bytes, SHA-256 `3bf41456f777045c5e76ce15a70580ef18303ac1f6160d3d1fe154649bce112c`. GPL.

The injector loads it at 0x80001800 as an extra text section of a Wii game's main.dol, appends the code list at 0x800022A8 (the handler's `lis`/`ori` at 0x104 name that address) and branches to its entry at 0x800018A8 from the game's VI retrace handler, the way Gecko OS and USB Loader GX hook it at runtime. Nothing else ships: no debugger stub, no multidol handler.
