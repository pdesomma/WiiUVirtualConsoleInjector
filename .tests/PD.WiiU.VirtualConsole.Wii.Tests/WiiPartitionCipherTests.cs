using WiiSharp;
using WiiUSharp.Nfs;

namespace PD.WiiU.VirtualConsole.Wii.Tests;

[TestClass]
public class WiiPartitionCipherTests
{
    private const long DataPartitionOffset = FakeDisc.DataPartitionOffset;
    private const int Clusters = FakeDisc.Clusters;
    private static readonly CommonKey CommonKey = FakeDisc.CommonKey;
    private static readonly NfsKey NfsKey = new(Enumerable.Range(1, 16).Select(i => (byte)(i * 5)).ToArray());

    [TestMethod]
    public void DecryptReportsTheDataPartitionSpan()
    {
        var span = new WiiPartitionCipher(CommonKey).Decrypt(new MemoryStream(FakeDisc.Build()), new MemoryStream(), CancellationToken.None);

        Assert.AreEqual(new DiscDataSpan(DataPartitionOffset, 0x20000 + Clusters * DiscFormat.ClusterSize), span);
    }

    [TestMethod]
    public void IsoSurvivesTheTripThroughNfsFiles()
    {
        var iso = FakeDisc.Build();
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

    private static byte[] Sectors(byte[] bytes, int first, int count) =>
        bytes.Skip(first * 0x8000).Take(count * 0x8000).ToArray();

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
