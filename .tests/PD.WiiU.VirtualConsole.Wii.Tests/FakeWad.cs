using WiiSharp;

namespace PD.WiiU.VirtualConsole.Wii.Tests;

/// <summary>
/// A channel WAD with only the signed parts filled in.
/// </summary>
internal static class FakeWad
{
    public static byte[] Build(byte[] titleId)
    {
        var ticket = Ticket.Build(titleId, new byte[16]).ToBytes();
        var tmd = Tmd.Build(titleId, 0x0000000100000035, 0x3031, RegionSettings.Preset(DiscRegion.UnitedStates), new TmdContent(0, 0, 1, 0x40, new byte[20])).ToBytes();
        var header = new byte[WadHeader.Size];
        Write(header, 0, WadHeader.Size);
        header[4] = 0x49;
        header[5] = 0x73;
        Write(header, 8, 0xA00);
        Write(header, 0x10, (uint)ticket.Length);
        Write(header, 0x14, (uint)tmd.Length);
        Write(header, 0x18, 0x40);

        var wad = new MemoryStream();
        Put(wad, header);
        Put(wad, new byte[0xA00]);
        Put(wad, ticket);
        Put(wad, tmd);
        Put(wad, new byte[0x40]);
        return wad.ToArray();
    }

    private static void Put(MemoryStream wad, byte[] section)
    {
        wad.Write(section, 0, section.Length);
        var padding = (WadHeader.Alignment - (int)(wad.Length % WadHeader.Alignment)) % WadHeader.Alignment;
        wad.Write(new byte[padding], 0, padding);
    }

    private static void Write(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
