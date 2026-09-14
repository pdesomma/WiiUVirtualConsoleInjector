# wiivc_chan_booter*.dol

FIX94's wiivc_chan_booter, release v1.0 (2017-10-01), unmodified, from https://github.com/FIX94/wiivc_chan_booter/releases/tag/v1.0. MIT.

| File | Bytes | SHA-256 |
|---|---|---|
| `wiivc_chan_booter.dol` | 167,904 | `862e77a088f1313eb106e3fc7bf44f0ba307d6cf7b9c6ee55fa68171bf77dde0` |
| `wiivc_chan_booter_force_4_by_3.dol` | 167,968 | `20ec94bd60ab8d5504792169f18ab84d6498aa1c07b3eafafa9c0f202647be64` |

The injector writes one of them as `main.dol` of a carrier disc together with `title.txt`, the four-character code of a channel the console already has installed; the booter launches that channel. The previous application shipped the default build as `forwarder.dol` (same bytes). `WiiOptions.ForwarderPath` accepts any other build.
