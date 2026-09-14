using System.Text;

namespace PD.WiiU.VirtualConsole.Retro.Tests;

/// <summary>
/// Builds a FrameLayout.arc with just the SARC and FLYT fields the patch reads.
/// </summary>
internal static class FakeFrameLayout
{
    public const int FrameOffset = LayoutOffset + 0x14 + 0x20;
    public const int LayoutOffset = 0x100;
    public const int MaskOffset = FrameOffset + 0x50;

    public static byte[] Build()
    {
        var archive = new byte[0x300];
        Encoding.ASCII.GetBytes("SARC").CopyTo(archive, 0);
        archive[6] = 0xFE;
        archive[7] = 0xFF;
        WriteUInt32(archive, 0x0C, 0x80);
        WriteUInt32(archive, 0x38, LayoutOffset - 0x80);

        Encoding.ASCII.GetBytes("FLYT").CopyTo(archive, LayoutOffset);
        archive[LayoutOffset + 4] = 0xFE;
        archive[LayoutOffset + 5] = 0xFF;
        archive[LayoutOffset + 6] = 0;
        archive[LayoutOffset + 7] = 0x14;
        WriteUInt32(archive, LayoutOffset + 0x0C, 0x14 + 0x20 + 0x50 + 0x30);

        Section(archive, LayoutOffset + 0x14, "lyt1", 0x20, "");
        Section(archive, FrameOffset, "pic1", 0x50, "frame");
        WriteUInt32(archive, FrameOffset + 0x2C, 0x42480000);
        WriteUInt32(archive, FrameOffset + 0x30, 0xC2480000);
        WriteUInt32(archive, FrameOffset + 0x44, 0x3F000000);
        WriteUInt32(archive, FrameOffset + 0x48, 0x3F000000);
        WriteUInt32(archive, FrameOffset + 0x4C, 0x44200000);
        Section(archive, MaskOffset, "pic1", 0x30, "frame_mask");
        archive[MaskOffset + 0x08] = 1;
        return archive;
    }

    private static void Section(byte[] archive, int at, string tag, int size, string name)
    {
        Encoding.ASCII.GetBytes(tag).CopyTo(archive, at);
        WriteUInt32(archive, at + 4, (uint)size);
        Encoding.ASCII.GetBytes(name).CopyTo(archive, at + 0x0C);
    }

    private static void WriteUInt32(byte[] data, int at, uint value)
    {
        data[at] = (byte)(value >> 24);
        data[at + 1] = (byte)(value >> 16);
        data[at + 2] = (byte)(value >> 8);
        data[at + 3] = (byte)value;
    }
}
