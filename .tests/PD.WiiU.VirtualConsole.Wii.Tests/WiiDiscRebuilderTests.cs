using WiiSharp;

namespace PD.WiiU.VirtualConsole.Wii.Tests;

[TestClass]
public class WiiDiscRebuilderTests
{
    [TestMethod]
    public void Rebuild_PlainDisc_KeepsSystemFilesFilesAndIdentity()
    {
        var plain = new MemoryStream(FakeRetailDisc.Plain(FakeRetailDisc.Dol()));
        var output = new MemoryStream();

        var result = WiiDiscRebuilder.Rebuild(plain, output);

        var disc = WiiDisc.Read(output);
        Assert.AreEqual(FakeRetailDisc.GameId, disc.Header.GameId);
        Assert.AreEqual(FakeRetailDisc.Title, disc.Header.Title);
        var partition = disc.DataPartitions[0];
        Assert.AreEqual(DiscFormat.RetailDataPartitionOffset, partition.Offset);
        Assert.AreEqual(DiscRegion.UnitedStates, RegionArea.Read(output).Region);
        CollectionAssert.AreEqual(new byte[] { 0, 1, 0, 0, 0x52, 0x53, 0x50, 0x45 }, result.Ticket.TitleId);

        var expected = FakeGameCube.BaseSystem();
        var system = PartitionSystemFiles.Read(output, partition);
        CollectionAssert.AreEqual(expected.Apploader.ToBytes(), system.Apploader.ToBytes());
        CollectionAssert.AreEqual(expected.CertificateChain, system.CertificateChain);

        var data = new PartitionDataStream(output, partition);
        var boot = system.Boot;
        CollectionAssert.AreEqual(FakeRetailDisc.Dol(), Read(data, Offset(boot, 0x420), 0x800));
        var files = Fst.Parse(Read(data, Offset(boot, 0x424), (int)Offset(boot, 0x428)));
        CollectionAssert.AreEqual(FakeRetailDisc.Files.Select(f => f.Path).ToArray(), files.Select(f => f.Path).ToArray());
        foreach (var (file, source) in files.Zip(FakeRetailDisc.Files, (f, s) => (f, s)))
        {
            Assert.AreEqual(0, file.Offset % WiiDiscRebuilder.FileAlignment);
            CollectionAssert.AreEqual(source.Content, Read(data, file.Offset, (int)file.Length), file.Path);
        }
    }

    [TestMethod]
    public void Rebuild_WithPatch_StoresTheReturnedDol()
    {
        var plain = new MemoryStream(FakeRetailDisc.Plain(FakeRetailDisc.Dol()));
        var output = new MemoryStream();
        byte[]? seen = null;

        WiiDiscRebuilder.Rebuild(plain, output, dol =>
        {
            seen = dol;
            var replaced = new byte[0x300];
            Array.Copy(dol, replaced, 0x100);
            replaced[0x93] = 0x02;
            for (var i = 0x100; i < replaced.Length; i++)
                replaced[i] = 0xAB;
            return replaced;
        });

        CollectionAssert.AreEqual(FakeRetailDisc.Dol(), seen);
        var partition = WiiDisc.Read(output).DataPartitions[0];
        var boot = PartitionSystemFiles.Read(output, partition).Boot;
        var stored = Read(new PartitionDataStream(output, partition), Offset(boot, 0x420), 0x300);
        Assert.IsTrue(stored.Skip(0x100).All(b => b == 0xAB));
    }

    [TestMethod]
    public void Rebuild_NkitDisc_ReadsTheBarePartition()
    {
        var nkit = new MemoryStream(FakeRetailDisc.Nkit(FakeRetailDisc.Dol(), DiscRegion.Europe));
        var output = new MemoryStream();

        var result = WiiDiscRebuilder.Rebuild(nkit, output);

        var disc = WiiDisc.Read(output);
        var partition = disc.DataPartitions[0];
        Assert.AreEqual(FakeRetailDisc.GameId, disc.Header.GameId);
        Assert.AreEqual(DiscRegion.Europe, RegionArea.Read(output).Region);
        Assert.AreEqual(partition.Offset, result.Partition.Start);
        var boot = PartitionSystemFiles.Read(output, partition).Boot;
        var data = new PartitionDataStream(output, partition);
        var files = Fst.Parse(Read(data, Offset(boot, 0x424), (int)Offset(boot, 0x428)));
        CollectionAssert.AreEqual(FakeRetailDisc.Files.Select(f => f.Path).ToArray(), files.Select(f => f.Path).ToArray());
        CollectionAssert.AreEqual(FakeRetailDisc.Files[0].Content, Read(data, files[0].Offset, (int)files[0].Length));
        Assert.IsFalse(WiiDiscRebuilder.IsNkit(output));
        Assert.IsTrue(WiiDiscRebuilder.IsNkit(nkit));
        Assert.IsFalse(WiiDiscRebuilder.IsNkit(new MemoryStream(new byte[16])));
        Assert.ThrowsExactly<ArgumentNullException>(() => WiiDiscRebuilder.IsNkit(null!));
    }

    [TestMethod]
    public void Rebuild_NullArguments_ThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => WiiDiscRebuilder.Rebuild(null!, new MemoryStream()));
        Assert.ThrowsExactly<ArgumentNullException>(() => WiiDiscRebuilder.Rebuild(new MemoryStream(), null!));
    }

    private static long Offset(byte[] boot, int at) =>
        (long)(uint)(boot[at] << 24 | boot[at + 1] << 16 | boot[at + 2] << 8 | boot[at + 3]) << 2;

    private static byte[] Read(Stream stream, long position, int count)
    {
        var bytes = new byte[count];
        stream.Position = position;
        var read = 0;
        while (read < count)
        {
            var n = stream.Read(bytes, read, count - read);
            Assert.AreNotEqual(0, n, "unexpected end of stream");
            read += n;
        }
        return bytes;
    }
}
