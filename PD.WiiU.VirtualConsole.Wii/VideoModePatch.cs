using PD.WiiU.VirtualConsole.Options;

namespace PD.WiiU.VirtualConsole.Wii;

/// <summary>
/// Rewrites every stock GXRModeObj in main.dol to another TV standard, keeping each mode's sample pattern and vertical filter.
/// </summary>
public static class VideoModePatch
{
    /// <summary>
    /// Bytes of a GXRModeObj before the sample pattern; the part that identifies the mode.
    /// </summary>
    public const int HeaderSize = 24;

    private const int Interlaced = 0;
    private const int NonInterlaced = 1;
    private const int Progressive = 2;

    private static readonly Standard[] SixtyHertz = { Standard.Ntsc, Standard.Mpal, Standard.Eurgb60 };
    private static readonly Geometry[] Geometries =
    {
        new(NonInterlaced, new Shape(240, 240, 0, 480, 0, 0, 0), new Shape(264, 264, 12, 528, 0, 0, 0)),
        new(NonInterlaced, new Shape(240, 240, 0, 480, 0, 0, 1), new Shape(264, 264, 12, 528, 0, 0, 1)),
        new(Interlaced, new Shape(240, 240, 0, 480, 0, 1, 0), new Shape(264, 264, 24, 528, 0, 1, 0)),
        new(Interlaced, new Shape(240, 240, 0, 480, 0, 1, 1), new Shape(264, 264, 24, 528, 0, 1, 1)),
        new(Interlaced, new Shape(480, 480, 0, 480, 1, 0, 0), new Shape(528, 528, 24, 528, 1, 0, 0), new Shape(480, 576, 0, 576, 1, 0, 0)),
        new(Interlaced, new Shape(242, 480, 0, 480, 1, 0, 1), new Shape(264, 524, 24, 524, 1, 0, 1)),
        new(Progressive, new Shape(480, 480, 0, 480, 0, 0, 0), new Shape(528, 528, 24, 528, 0, 0, 0), new Shape(480, 576, 0, 576, 0, 0, 0)),
        new(Progressive, new Shape(242, 480, 0, 480, 0, 0, 1), new Shape(264, 524, 24, 524, 0, 0, 1)),
    };
    private static readonly Dictionary<string, (Geometry Geometry, Standard Standard)> Known = BuildTable();

    /// <summary>
    /// Patches in place.
    /// </summary>
    /// <param name="dol">main.dol.</param>
    /// <param name="target">Standard to force; <see cref="WiiVideoMode.Unchanged"/> does nothing.</param>
    /// <returns>Modes rewritten.</returns>
    public static int Apply(byte[] dol, WiiVideoMode target)
    {
        if (dol is null)
            throw new ArgumentNullException(nameof(dol));
        if (target == WiiVideoMode.Unchanged)
            return 0;

        var standard = target switch
        {
            WiiVideoMode.Ntsc => Standard.Ntsc,
            WiiVideoMode.Pal50 => Standard.Pal,
            WiiVideoMode.Pal60 => Standard.Eurgb60,
            _ => throw new ArgumentOutOfRangeException(nameof(target)),
        };

        var count = 0;
        var header = new byte[HeaderSize];
        for (var at = 0; at + HeaderSize <= dol.Length; at += 4)
        {
            if (dol[at + 4] != 0x02 || dol[at + 5] != 0x80 || dol[at + 10] != 0x00 || dol[at + 11] != 0x28)
                continue;

            Array.Copy(dol, at, header, 0, HeaderSize);
            if (!Known.TryGetValue(Key(header), out var mode))
                continue;

            var replacement = mode.Geometry.Encode(standard);
            if (replacement.SequenceEqual(header))
                continue;
            replacement.CopyTo(dol, at);
            count++;
        }
        return count;
    }

    private static Dictionary<string, (Geometry, Standard)> BuildTable()
    {
        var table = new Dictionary<string, (Geometry, Standard)>(StringComparer.Ordinal);
        foreach (var geometry in Geometries)
        {
            foreach (var standard in SixtyHertz)
                table[Key(geometry.Encode(standard))] = (geometry, standard);
            table[Key(geometry.Encode(Standard.Pal))] = (geometry, Standard.Pal);
            if (geometry.PalScaled is not null)
                table[Key(geometry.Encode(Standard.Pal, scaled: true))] = (geometry, Standard.Pal);
        }
        return table;
    }

    private static string Key(byte[] header) => BitConverter.ToString(header);

    private enum Standard
    {
        Ntsc = 0,
        Pal = 1,
        Mpal = 2,
        Eurgb60 = 5,
    }

    private sealed record Shape(ushort EfbHeight, ushort XfbHeight, ushort ViYOrigin, ushort ViHeight, uint XfbMode, byte FieldRendering, byte Aa);

    private sealed class Geometry
    {
        private readonly int _mode;
        private readonly Shape _pal;
        private readonly Shape _sixtyHertz;

        public Geometry(int mode, Shape sixtyHertz, Shape pal, Shape? palScaled = null)
        {
            _mode = mode;
            _sixtyHertz = sixtyHertz;
            _pal = pal;
            PalScaled = palScaled;
        }

        public Shape? PalScaled { get; }

        public byte[] Encode(Standard standard, bool scaled = false)
        {
            var shape = standard == Standard.Pal ? (scaled ? PalScaled! : _pal) : _sixtyHertz;
            var bytes = new byte[HeaderSize];
            WriteUInt32(bytes, 0, (uint)((int)standard << 2 | _mode));
            WriteUInt16(bytes, 4, 640);
            WriteUInt16(bytes, 6, shape.EfbHeight);
            WriteUInt16(bytes, 8, shape.XfbHeight);
            WriteUInt16(bytes, 10, 40);
            WriteUInt16(bytes, 12, shape.ViYOrigin);
            WriteUInt16(bytes, 14, 640);
            WriteUInt16(bytes, 16, shape.ViHeight);
            WriteUInt32(bytes, 18, shape.XfbMode);
            bytes[22] = shape.FieldRendering;
            bytes[23] = shape.Aa;
            return bytes;
        }

        private static void WriteUInt16(byte[] bytes, int offset, ushort value)
        {
            bytes[offset] = (byte)(value >> 8);
            bytes[offset + 1] = (byte)value;
        }

        private static void WriteUInt32(byte[] bytes, int offset, uint value)
        {
            bytes[offset] = (byte)(value >> 24);
            bytes[offset + 1] = (byte)(value >> 16);
            bytes[offset + 2] = (byte)(value >> 8);
            bytes[offset + 3] = (byte)value;
        }
    }
}
