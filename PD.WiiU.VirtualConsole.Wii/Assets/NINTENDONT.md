# nintendont_*_autobooter.dol

FIX94's Nintendont autoboot forwarder, release v1.2 (2017-09-28), unmodified, from https://github.com/FIX94/nintendont-autoboot-forwarder/releases/tag/v1.2. MIT.

| File | Bytes | SHA-256 |
|---|---|---|
| `nintendont_default_autobooter.dol` | 198,336 | `6eff4c9b1bf4afe8383a06613c431bb9a105863a3be1d13320621b8a11742c5e` |
| `nintendont_force_4_by_3_autobooter.dol` | 198,336 | `31bd40282564dd839f1f52dcf3309117e11a4f2549db3c70a98fee5bbc53e7ec` |

The injector writes one of them as `main.dol` of the carrier disc. On the console it loads Nintendont from `sd:/apps/nintendont/boot.dol` and autoboots `game.iso` from the disc; Nintendont itself is not shipped. The previous application called the same two builds `nintendont.dol` and `nintendont_force.dol`; `GameCubeOptions.ForwarderPath` accepts any other build.
