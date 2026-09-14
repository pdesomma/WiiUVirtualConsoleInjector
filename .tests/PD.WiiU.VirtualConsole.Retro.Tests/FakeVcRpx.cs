using System.Text;
using WiiUSharp.Rpx;

namespace PD.WiiU.VirtualConsole.Retro.Tests;

/// <summary>
/// Builds a plain RPX shaped like a NES or SNES Virtual Console executable: display-size code in .text, a WUP- ROM slot in .rodata.
/// </summary>
internal static class FakeVcRpx
{
    public const byte SizeKey = 0x02;
    public const int SlotCapacity = 0x20000;

    public static readonly byte[] PadPattern = { 0x39, 0x20, 0x03, 0x56, 0x38, 0x60 };
    public static readonly byte[] TvPattern = { 0x04, 0x38, 0x38, 0xE0, 0x07, 0x80, 0x90 };

    public static byte[] Build(bool nes, byte[]? baseRom = null)
    {
        var names = Encoding.ASCII.GetBytes("\0.text\0.rodata\0.shstrtab\0.rplcrcs\0.rplfileinfo\0");
        var text = new byte[0x3000];
        for (var i = 0; i < text.Length; i++)
            text[i] = (byte)(i * 13);
        TvPattern.CopyTo(text, 0x100);
        PadPattern.CopyTo(text, 0x200);
        PadPattern.CopyTo(text, 0x2800);

        var rodata = Rodata(nes, baseRom);
        var sections = new (string Name, SectionType Type, byte[] Data, uint Size)[]
        {
            ("", SectionType.Null, Array.Empty<byte>(), 0u),
            (".text", SectionType.ProgBits, text, (uint)text.Length),
            (".rodata", SectionType.ProgBits, rodata, (uint)rodata.Length),
            (".shstrtab", SectionType.StrTab, names, (uint)names.Length),
            (".rplcrcs", SectionType.RplCrcs, new byte[6 * 4], 6u * 4),
            (".rplfileinfo", SectionType.RplFileInfo, new byte[0x60], 0x60u),
        };

        var output = new MemoryStream();
        var header = new byte[RpxFormat.HeaderSize];
        header[0] = 0x7F;
        header[1] = 0x45;
        header[2] = 0x4C;
        header[3] = 0x46;
        header[4] = 1;
        header[5] = 2;
        header[6] = 1;
        Write16(header, 0x10, RpxFormat.RplType);
        Write16(header, 0x12, 0x14);
        Write32(header, 0x14, 1);
        Write32(header, 0x20, 0x40);
        Write16(header, 0x28, RpxFormat.HeaderSize);
        Write16(header, 0x2E, RpxFormat.SectionHeaderSize);
        Write16(header, 0x30, (ushort)sections.Length);
        Write16(header, 0x32, 3);
        output.Write(header, 0, header.Length);
        while (output.Position < 0x40 + sections.Length * RpxFormat.SectionHeaderSize)
            output.WriteByte(0);

        var headers = new byte[sections.Length][];
        var position = ((uint)output.Position + 0x3Fu) & ~0x3Fu;
        for (var i = 0; i < sections.Length; i++)
        {
            var (name, type, data, size) = sections[i];
            var shdr = new byte[RpxFormat.SectionHeaderSize];
            Write32(shdr, 0x00, NameOffset(names, name));
            Write32(shdr, 0x04, (uint)type);
            Write32(shdr, 0x14, size);
            if (type != SectionType.Null)
            {
                Write32(shdr, 0x10, position);
                output.Position = position;
                output.Write(data, 0, data.Length);
                position = ((uint)output.Position + 0x3Fu) & ~0x3Fu;
            }
            headers[i] = shdr;
        }
        output.Position = 0x40;
        foreach (var shdr in headers)
            output.Write(shdr, 0, shdr.Length);
        return output.ToArray();
    }

    public static byte[] NesRom(int length)
    {
        var rom = new byte[length];
        rom[0] = 0x4E;
        rom[1] = 0x45;
        rom[2] = 0x53;
        rom[3] = 0x1A;
        for (var i = 4; i < length; i++)
            rom[i] = (byte)(i * 7);
        return rom;
    }

    /// <summary>
    /// Offset of the ROM inside .rodata.
    /// </summary>
    public static int RomOffset(bool nes) => 0x40 + (nes ? 16 : 12);

    public static byte[] SnesRom(int length)
    {
        var rom = new byte[length];
        for (var i = 0; i < length; i++)
            rom[i] = (byte)(0x80 + i * 3);
        return rom;
    }

    private static uint NameOffset(byte[] names, string name)
    {
        if (name.Length == 0)
            return 0;
        var needle = Encoding.ASCII.GetBytes(name + "\0");
        for (var i = 0; i + needle.Length <= names.Length; i++)
            if (names.Skip(i).Take(needle.Length).SequenceEqual(needle))
                return (uint)i;
        throw new ArgumentException(name);
    }

    private static byte[] Rodata(bool nes, byte[]? baseRom)
    {
        var marker = 0x40;
        var rodata = new byte[marker + 16 + SlotCapacity + 16 + 0x100];
        for (var i = 0; i < rodata.Length; i++)
            rodata[i] = 0xEE;
        rodata[marker - (nes ? 14 : 22)] = SizeKey;
        Encoding.ASCII.GetBytes("WUP-JAAE").CopyTo(rodata, marker);
        Array.Clear(rodata, marker + 8, 8);
        if (nes)
            new byte[] { 0x4E, 0x45, 0x53, 0x1A }.CopyTo(rodata, marker + 16);
        var rom = baseRom ?? (nes ? NesRom(0x8010) : SnesRom(0x8000));
        rom.CopyTo(rodata, RomOffset(nes));
        return rodata;
    }

    private static void Write16(byte[] bytes, int offset, ushort value)
    {
        bytes[offset] = (byte)(value >> 8);
        bytes[offset + 1] = (byte)value;
    }

    private static void Write32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
