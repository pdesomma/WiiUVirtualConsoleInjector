using System.Security.Cryptography;
using WiiSharp;
using WiiUSharp.Nfs;

namespace PD.WiiU.VirtualConsole.Wii.Tests;

[TestClass]
public class WiiPartitionCipherTests
{
    private const long DataPartitionOffset = 0x60000;
    private const long DataStart = DataPartitionOffset + 0x20000;
    private const int Clusters = 3;
    private static readonly CommonKey CommonKey = new(Enumerable.Range(1, 16).Select(i => (byte)(i * 9)).ToArray());
    private static readonly NfsKey NfsKey = new(Enumerable.Range(1, 16).Select(i => (byte)(i * 5)).ToArray());
    private static readonly byte[] TitleId = { 0x00, 0x01, 0x00, 0x00, 0x41, 0x42, 0x43, 0x44 };
    private static readonly TitleKey TitleKey = new(Enumerable.Range(1, 16).Select(i => (byte)(i * 13)).ToArray());

    [TestMethod]
    public void DecryptReportsTheDataPartitionSpan()
    {
        var span = new WiiPartitionCipher(CommonKey).Decrypt(new MemoryStream(BuildIso()), new MemoryStream(), CancellationToken.None);

        Assert.AreEqual(new DiscDataSpan(DataPartitionOffset, 0x20000 + Clusters * DiscFormat.ClusterSize), span);
    }

    [TestMethod]
    public void IsoSurvivesTheTripThroughNfsFiles()
    {
        var iso = BuildIso();
        var root = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Wii.Tests", Guid.NewGuid().ToString("N"));
        try
        {
            var converter = new NfsConverter(new WiiPartitionCipher(CommonKey));
            var files = converter.FromIso(new MemoryStream(iso), NfsKey, Path.Combine(root, "content"), Path.Combine(root, "temp"));

            var restored = new PrefixStream(iso.Length);
            converter.ToIso(Path.Combine(root, "content"), NfsKey, restored);

            Assert.AreEqual(1, files.Count);
            Assert.AreEqual(NfsConverter.SingleLayerSize, restored.Length);
            var actual = restored.Prefix;
            CollectionAssert.AreEqual(Sectors(iso, 0, 1), Sectors(actual, 0, 1), "disc header");
            CollectionAssert.AreEqual(Sectors(iso, 8, 2), Sectors(actual, 8, 2), "partition tables");
            var dataSectors = (int)((0x20000 + Clusters * DiscFormat.ClusterSize) / 0x8000);
            CollectionAssert.AreEqual(Sectors(iso, (int)(DataPartitionOffset / 0x8000), dataSectors), Sectors(actual, (int)(DataPartitionOffset / 0x8000), dataSectors), "data partition");
            CollectionAssert.AreEqual(new byte[0x8000], Sectors(actual, 1, 1), "gap is zeroed");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static byte[] BuildIso()
    {
        var iso = new byte[DataStart + Clusters * DiscFormat.ClusterSize];
        System.Text.Encoding.ASCII.GetBytes("RABCDE").CopyTo(iso, 0);
        WriteUInt32(iso, 0x18, DiscFormat.WiiMagic);
        WriteUInt32(iso, 0x40000, 1);
        WriteUInt32(iso, 0x40004, 0x40020 >> 2);
        WriteUInt32(iso, 0x40020, (uint)(DataPartitionOffset >> 2));
        WriteUInt32(iso, 0x40024, (uint)PartitionType.Data);

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
        WriteUInt32(iso, (int)DataPartitionOffset + Ticket.Size + 0x14, 0x20000 >> 2);
        WriteUInt32(iso, (int)DataPartitionOffset + Ticket.Size + 0x18, (uint)((Clusters * DiscFormat.ClusterSize) >> 2));

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

    private static byte[] Sectors(byte[] bytes, int first, int count) =>
        bytes.Skip(first * 0x8000).Take(count * 0x8000).ToArray();

    private static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

    private sealed class PrefixStream : Stream
    {
        private readonly MemoryStream _prefix = new();
        private readonly long _keep;
        private long _position;

        public PrefixStream(long keep)
        {
            _keep = keep;
        }

        public byte[] Prefix => _prefix.ToArray();
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _position;
        public override long Position { get => _position; set => throw new NotSupportedException(); }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            var keep = (int)Math.Max(0, Math.Min(count, _keep - _prefix.Length));
            _prefix.Write(buffer, offset, keep);
            _position += count;
        }
    }
}
