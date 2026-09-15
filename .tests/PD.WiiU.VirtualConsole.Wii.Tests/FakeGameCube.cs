using System.IO.Compression;
using System.Text;
using WiiSharp;

namespace PD.WiiU.VirtualConsole.Wii.Tests;

/// <summary>
/// Tiny GameCube images, plain and GCZ, plus a plaintext base disc for the carrier to copy from.
/// </summary>
internal static class FakeGameCube
{
    public const string GameId = "GALE01";
    public const string Title = "Super Smash Bros Melee";
    public const int BlockSize = 0x400;

    /// <summary>
    /// Where the one-file table sits.
    /// </summary>
    public const int FstOffset = 0x500;
    /// <summary>
    /// Where the one file starts; the zero gap ahead of it compacts to a record.
    /// </summary>
    public const int FileOffset = 0x600;

    /// <summary>
    /// A tiny but well-formed image: header, a one-file table, a zero gap and the file to the end.
    /// </summary>
    /// <param name="length">Whole image length.</param>
    /// <param name="seed">Varies the header and payload bytes.</param>
    public static byte[] Image(int length = 0x2345, byte seed = 1)
    {
        var bytes = Enumerable.Range(0, length).Select(i => (byte)(i * seed + 3)).ToArray();
        Encoding.ASCII.GetBytes(GameId).CopyTo(bytes, 0);
        bytes[6] = (byte)(seed - 1);
        bytes[7] = 0;
        bytes[0x1C] = 0xC2;
        bytes[0x1D] = 0x33;
        bytes[0x1E] = 0x9F;
        bytes[0x1F] = 0x3D;
        Array.Clear(bytes, 0x20, 0x420);
        Encoding.ASCII.GetBytes(Title).CopyTo(bytes, 0x20);

        var fst = Fst.Build(new[] { new FstFile("data.bin", FileOffset << 2, length - FileOffset) });
        WriteUInt32(bytes, 0x420, 0x440);
        WriteUInt32(bytes, 0x424, FstOffset);
        WriteUInt32(bytes, 0x428, (uint)fst.Length);
        WriteUInt32(bytes, 0x42C, (uint)fst.Length);
        fst.CopyTo(bytes, FstOffset);
        Array.Clear(bytes, FstOffset + fst.Length, FileOffset - FstOffset - fst.Length);
        return bytes;
    }

    /// <summary>
    /// The NKit form of <see cref="Image"/>, as the injector stores it.
    /// </summary>
    /// <param name="image">Full image.</param>
    public static byte[] Compact(byte[] image)
    {
        var output = new MemoryStream();
        NkitGameCube.Compact(new MemoryStream(image), output);
        return output.ToArray();
    }

    public static byte[] Gcz(byte[] plain)
    {
        var blocks = (plain.Length + BlockSize - 1) / BlockSize;
        var pointers = new List<ulong>();
        var hashes = new List<uint>();
        var data = new MemoryStream();
        for (var i = 0; i < blocks; i++)
        {
            var slice = plain.Skip(i * BlockSize).Take(BlockSize).ToArray();
            var stored = i % 2 == 1;
            var bytes = stored ? slice : Zlib(slice);
            pointers.Add((ulong)data.Length | (stored ? GczFile.StoredFlag : 0));
            hashes.Add(Adler(bytes));
            data.Write(bytes, 0, bytes.Length);
        }

        var image = new MemoryStream();
        image.Write(BitConverter.GetBytes(GczHeader.Magic), 0, 4);
        image.Write(BitConverter.GetBytes(0u), 0, 4);
        image.Write(BitConverter.GetBytes((ulong)data.Length), 0, 8);
        image.Write(BitConverter.GetBytes((ulong)plain.Length), 0, 8);
        image.Write(BitConverter.GetBytes((uint)BlockSize), 0, 4);
        image.Write(BitConverter.GetBytes((uint)blocks), 0, 4);
        foreach (var pointer in pointers)
            image.Write(BitConverter.GetBytes(pointer), 0, 8);
        foreach (var hash in hashes)
            image.Write(BitConverter.GetBytes(hash), 0, 4);
        data.WriteTo(image);
        return image.ToArray();
    }

    public static PartitionSystemFiles BaseSystem()
    {
        var boot = new byte[DiscFormat.BootSize];
        Encoding.ASCII.GetBytes("RBASE1").CopyTo(boot, 0);
        Write(boot, 0x18, DiscFormat.WiiMagic);
        Encoding.ASCII.GetBytes("Base Game").CopyTo(boot, 0x20);
        Write(boot, 0x430, 0x817E5A00);
        Write(boot, 0x434, 0x1A600);

        var bi2 = new byte[DiscFormat.Bi2Size];
        Write(bi2, 0x1C, 1);
        Write(bi2, 0x2C, 0x04000000);

        var apploader = new byte[Apploader.HeaderSize + 0x80 + 0x20];
        Encoding.ASCII.GetBytes("2010/10/01").CopyTo(apploader, 0);
        Write(apploader, 0x10, 0x81200000);
        Write(apploader, 0x14, 0x80);
        Write(apploader, 0x18, 0x20);
        for (var i = Apploader.HeaderSize; i < apploader.Length; i++)
            apploader[i] = (byte)(0x5A ^ i);

        var chain = Enumerable.Range(0, 0xA00).Select(i => (byte)(i * 7)).ToArray();
        return new PartitionSystemFiles(boot, bi2, Apploader.Parse(apploader), chain);
    }

    public static byte[] BaseDisc(DiscRegion region)
    {
        var builder = new WiiDiscBuilder("RBASE1", "Base Game", BaseSystem(), new byte[] { 0xBA, 0x5E })
        {
            PartitionOffset = 0x50000,
            Region = RegionSettings.Preset(region),
        };
        builder.Files.Add(new DiscFile("base.bin", new MemoryStream(new byte[0x100])));
        var output = new MemoryStream();
        builder.Build(output);
        return output.ToArray();
    }

    private static uint Adler(byte[] bytes)
    {
        uint a = 1, b = 0;
        foreach (var x in bytes)
        {
            a = (a + x) % 65521;
            b = (b + a) % 65521;
        }
        return b << 16 | a;
    }

    private static void Write(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

    private static byte[] Zlib(byte[] plain)
    {
        var output = new MemoryStream();
        output.WriteByte(0x78);
        output.WriteByte(0x9C);
        using (var deflate = new DeflateStream(output, CompressionMode.Compress, leaveOpen: true))
            deflate.Write(plain, 0, plain.Length);
        var adler = Adler(plain);
        output.Write(new[] { (byte)(adler >> 24), (byte)(adler >> 16), (byte)(adler >> 8), (byte)adler }, 0, 4);
        return output.ToArray();
    }

    private static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
