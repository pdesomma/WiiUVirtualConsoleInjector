# Template/*

The title skeleton a core is dropped into, taken from dimok789's Homebrew Launcher channel (`homebrew_launcher_channel`, v1.4, GPL-3.0, https://github.com/dimok789/homebrew_launcher/releases/tag/v1.4): its full-permission cos.xml (with `max_codesize` raised to 0x03000000 so a core fits), app.xml and meta.xml with the identity blanked, and the boot art by cathor and Maschell. The user's own artwork and boot sound replace the images and sound at inject time; the movie stays.

| File | Bytes | SHA-256 |
|---|---|---|
| `app.xml` | 638 | `d5456dad7f57c0a1d92c24b50fc5d18f9269af3d7cde40f4ef4d5ccf6e8bdaef` |
| `cos.xml` | 3,904 | `92c0fc9785ecd24ac4b099b36831cdaaf93425fbda99f58cb29a82ebe734814f` |
| `meta.xml` | 9,600 | `bbc7c283e96eafa9087b6b345972cf60927cf45435af7991aa47f5b853abd8bc` |
| `iconTex.tga` | 65,580 | `a2017749623c8d94d307d2eeb17a64bcf6a6f85132b9c5a176b76655abfbb0fc` |
| `bootTvTex.tga` | 2,764,844 | `2793392ad713ce722c43a4aa73e136cf98e75293bf255a886c7c9318fe6a3daf` |
| `bootDrcTex.tga` | 1,229,804 | `904f9764ebe7e0d9a18c0dad61394d098784d535b720b5bc90d0a06711d31003` |
| `bootLogoTex.tga` | 28,604 | `eced35ca4ad971c4f6970140db3d4f7d60a0308553b3650219bed07f691f6a25` |
| `bootMovie.h264` | 52,530 | `c1109617d9e33ce2987b35a16db312007098429a0a65a38ecffa566b29393f8b` |
| `bootSound.btsnd` | 450,076 | `5c29b8aa001742c1d6d28e8738afe8c6759c40430cd880150909e66c9a96a128` |

`title_id`, `group_id`, `product_code` and every name are placeholders; `MetaXml.Apply`/`AppXml.Apply` fill them at inject time. `argstr` is empty until the injector sets it.
