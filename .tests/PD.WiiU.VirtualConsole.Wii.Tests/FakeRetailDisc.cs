using System.Security.Cryptography;
using WiiSharp;

namespace PD.WiiU.VirtualConsole.Wii.Tests;

/// <summary>
/// A small but structurally complete Wii game disc: system files, a DOL carrying every patchable pattern, an FST and files.
/// </summary>
internal static class FakeRetailDisc
{
    public const string GameId = "RSPE01";
    public const string Title = "Retail Game";
    public const long PartitionOffset = 0x50000;
    public static readonly (string Path, byte[] Content)[] Files =
    {
        ("opening.bnr", Enumerable.Range(0, 0x1234).Select(i => (byte)(i * 3)).ToArray()),
        ("data/level1.arc", Enumerable.Range(0, 0x777).Select(i => (byte)(i ^ 0x55)).ToArray()),
        ("data/sound/bgm.brstm", Enumerable.Range(0, 0x40).Select(i => (byte)(0xC0 - i)).ToArray()),
    };

    public static readonly byte[] Ntsc480IntDfHeader = { 0, 0, 0, 0, 0x02, 0x80, 0x01, 0xE0, 0x01, 0xE0, 0x00, 0x28, 0x00, 0x00, 0x02, 0x80, 0x01, 0xE0, 0, 0, 0, 1, 0, 0 };
    public static readonly byte[] Pal528IntDfHeader = { 0, 0, 0, 4, 0x02, 0x80, 0x02, 0x10, 0x02, 0x10, 0x00, 0x28, 0x00, 0x18, 0x02, 0x80, 0x02, 0x10, 0, 0, 0, 1, 0, 0 };
    public static readonly byte[] SamplePattern = Enumerable.Repeat((byte)0x66, 24).ToArray();
    public static readonly byte[] DefaultVerticalFilter = { 8, 8, 10, 12, 10, 8, 8 };

    public const int DeflickerOffset = 0x200;
    public const int DitheringOffset = 0x400;
    public const int VerticalFilterOffset = 0x600;
    public const int RenderModeOffset = 0x700;
    /// <summary>
    /// Where the VI retrace handler's first instructions sit; its blr is 0x10 bytes further on.
    /// </summary>
    public const int ViHookOffset = 0x380;
    public static readonly byte[] ViRetraceStart = { 0x7C, 0xE3, 0x3B, 0x78, 0x38, 0x87, 0x00, 0x34, 0x38, 0xA7, 0x00, 0x38, 0x38, 0xC7, 0x00, 0x4C };

    /// <summary>
    /// DOL body with the deflicker, dithering, vertical filter and render mode patterns at fixed offsets.
    /// </summary>
    public static byte[] Dol()
    {
        var dol = new byte[0x800];
        WriteUInt32(dol, 0x00, 0x100);
        WriteUInt32(dol, 0x48, 0x80004000);
        WriteUInt32(dol, 0x90, 0x700);
        WriteUInt32(dol, 0xE0, 0x80004000);
        for (var i = 0x100; i < dol.Length; i++)
            dol[i] = (byte)(0x10 + i % 7);

        ViRetraceStart.CopyTo(dol, ViHookOffset);
        new byte[] { 0x4E, 0x80, 0x00, 0x20 }.CopyTo(dol, ViHookOffset + 0x10);
        DolFilterPatches.DeflickerPattern.CopyTo(dol, DeflickerOffset);
        new byte[] { 0x7C, 0x08, 0x02, 0xA6 }.CopyTo(dol, DitheringOffset - 4);
        DolFilterPatches.DitheringPattern.CopyTo(dol, DitheringOffset);
        DefaultVerticalFilter.CopyTo(dol, VerticalFilterOffset);
        Ntsc480IntDfHeader.CopyTo(dol, RenderModeOffset);
        SamplePattern.CopyTo(dol, RenderModeOffset + 24);
        DefaultVerticalFilter.CopyTo(dol, RenderModeOffset + 48);
        return dol;
    }

    /// <summary>
    /// Plaintext disc with the given main.dol.
    /// </summary>
    public static byte[] Plain(byte[] dol, DiscRegion region = DiscRegion.UnitedStates)
    {
        var builder = new WiiDiscBuilder(GameId, Title, FakeGameCube.BaseSystem(), dol)
        {
            PartitionOffset = PartitionOffset,
            Region = RegionSettings.Preset(region),
            EncryptedTitleKey = WrappedTitleKey(),
            FileAlignment = 0x20,
        };
        foreach (var (path, content) in Files)
            builder.Files.Add(new DiscFile(path, new MemoryStream(content)));
        var output = new MemoryStream();
        builder.Build(output);
        return output.ToArray();
    }

    /// <summary>
    /// The same disc as a retail-style encrypted image.
    /// </summary>
    /// <summary>
    /// The disc as NKit stores it: plaintext partition with the hash blocks stripped, its length rewritten, the NKit block at 0x200.
    /// </summary>
    /// <param name="dol">main.dol to place.</param>
    /// <param name="region">Region area preset.</param>
    public static byte[] Nkit(byte[] dol, DiscRegion region = DiscRegion.UnitedStates)
    {
        var plain = Plain(dol, region);
        var partition = WiiDisc.Read(new MemoryStream(plain)).DataPartitions[0];
        var output = new MemoryStream();
        output.Write(plain, 0, (int)partition.DataStart);
        var clusters = (int)(partition.Header.DataSize / DiscFormat.ClusterSize);
        for (var c = 0; c < clusters; c++)
            output.Write(plain, (int)(partition.DataStart + c * DiscFormat.ClusterSize + DiscFormat.ClusterHashSize), DiscFormat.ClusterDataSize);
        var bytes = output.ToArray();
        WriteUInt32(bytes, (int)(partition.Offset + 0x2BC), (uint)((clusters * DiscFormat.ClusterDataSize) >> 2));
        bytes[0x60] = 1;
        bytes[0x61] = 1;
        new NkitHeader(isWii: true, sourceCrc: 0x12345678, sourceLength: plain.Length).Write(bytes);
        return bytes;
    }

    public static byte[] Encrypted(byte[] dol, DiscRegion region = DiscRegion.UnitedStates)
    {
        var output = new MemoryStream();
        new DiscCipher(FakeDisc.CommonKey).Encrypt(new MemoryStream(Plain(dol, region)), output);
        return output.ToArray();
    }

    private static byte[] WrappedTitleKey()
    {
        var iv = new byte[16];
        new byte[] { 0, 1, 0, 0, 0x52, 0x53, 0x50, 0x45 }.CopyTo(iv, 0);
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        aes.Key = FakeDisc.CommonKey.ToArray();
        aes.IV = iv;
        using var t = aes.CreateEncryptor();
        return t.TransformFinalBlock(FakeDisc.TitleKey.ToArray(), 0, 16);
    }

    private static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
