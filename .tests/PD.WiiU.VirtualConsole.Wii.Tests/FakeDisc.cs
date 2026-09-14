using System.Security.Cryptography;
using System.Text;
using WiiSharp;

namespace PD.WiiU.VirtualConsole.Wii.Tests;

/// <summary>
/// Builds a tiny encrypted Wii disc image with one data partition.
/// </summary>
internal static class FakeDisc
{
    public const int Clusters = 3;
    public const long DataPartitionOffset = 0x60000;
    public const long DataStart = DataPartitionOffset + 0x20000;
    public const string GameId = "RABCDE";
    public const int TmdSize = 0x40;
    public static readonly CommonKey CommonKey = new(Enumerable.Range(1, 16).Select(i => (byte)(i * 9)).ToArray());
    public static readonly byte[] TitleId = { 0x00, 0x01, 0x00, 0x00, 0x41, 0x42, 0x43, 0x44 };
    public static readonly TitleKey TitleKey = new(Enumerable.Range(1, 16).Select(i => (byte)(i * 13)).ToArray());

    public static long TmdOffset => DataPartitionOffset + Ticket.Size + PartitionHeader.Size;

    public static byte[] Build()
    {
        var iso = new byte[DataStart + Clusters * DiscFormat.ClusterSize];
        Encoding.ASCII.GetBytes(GameId).CopyTo(iso, 0);
        WriteUInt32(iso, 0x18, DiscFormat.WiiMagic);
        WriteUInt32(iso, 0x40000, 1);
        WriteUInt32(iso, 0x40004, 0x40020 >> 2);
        WriteUInt32(iso, 0x40020, (uint)(DataPartitionOffset >> 2));
        WriteUInt32(iso, 0x40024, (uint)PartitionType.Data);
        RegionSettings.Preset(DiscRegion.Japan).ToBytes().CopyTo(iso, RegionArea.Offset);

        var iv = new byte[16];
        TitleId.CopyTo(iv, 0);
        using (var aes = Aes.Create())
        {
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            aes.Key = CommonKey.ToArray();
            aes.IV = iv;
            using var t = aes.CreateEncryptor();
            t.TransformFinalBlock(TitleKey.ToArray(), 0, 16).CopyTo(iso, DataPartitionOffset + Ticket.EncryptedTitleKeyOffset);
        }
        TitleId.CopyTo(iso, DataPartitionOffset + Ticket.TitleIdOffset);
        WriteUInt32(iso, (int)DataPartitionOffset + Ticket.Size + 0x00, TmdSize);
        WriteUInt32(iso, (int)DataPartitionOffset + Ticket.Size + 0x04, (uint)((TmdOffset - DataPartitionOffset) >> 2));
        WriteUInt32(iso, (int)DataPartitionOffset + Ticket.Size + 0x14, 0x20000 >> 2);
        WriteUInt32(iso, (int)DataPartitionOffset + Ticket.Size + 0x18, (uint)((Clusters * DiscFormat.ClusterSize) >> 2));
        Tmd().CopyTo(iso, TmdOffset);

        using var cipher = new ClusterCipher(TitleKey);
        for (var c = 0; c < Clusters; c++)
        {
            var cluster = new byte[DiscFormat.ClusterSize];
            for (var i = 0; i < cluster.Length; i++)
                cluster[i] = (byte)(c * 37 + i * 11);
            cipher.Encrypt(cluster);
            cluster.CopyTo(iso, DataStart + c * DiscFormat.ClusterSize);
        }
        return iso;
    }

    public static byte[] TicketBytes() => Build().Skip((int)DataPartitionOffset).Take(Ticket.Size).ToArray();

    public static byte[] Tmd() => Enumerable.Range(0, TmdSize).Select(i => (byte)(0xA0 + i)).ToArray();

    private static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
