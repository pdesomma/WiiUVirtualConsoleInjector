using PD.WiiU.VirtualConsole.Options;
using WiiSharp;
using WiiUSharp;
using WiiUSharp.Nfs;

namespace PD.WiiU.VirtualConsole.Wii.Tests;

[TestClass]
public class WiiRomInjectorTests
{
    private const string MetaXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><menu type=\"complex\" access=\"777\"><reserved_flag2 type=\"hexBinary\" length=\"4\">00000000</reserved_flag2></menu>";
    private static readonly NfsKey NfsKey = new(Enumerable.Range(1, 16).Select(i => (byte)(i * 5)).ToArray());

    private string _root = null!;

    [TestInitialize]
    public void Initialize() => _root = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Wii.Tests", Guid.NewGuid().ToString("N"));

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void Console_Always_IsWii()
    {
        Assert.AreEqual(SourceConsole.Wii, new WiiRomInjector(FakeDisc.CommonKey).Console);
    }

    [TestMethod]
    public async Task InjectAsync_Iso_WritesNfsTicketTmdFirmwareAndManualId()
    {
        var title = StageBase();
        var messages = new List<string>();

        await new WiiRomInjector(FakeDisc.CommonKey).InjectAsync(Injection(), title, new SyncProgress(messages.Add));

        CollectionAssert.AreEqual(new[] { "hif_000000.nfs" }, Directory.GetFiles(title.Content).Select(f => Path.GetFileName(f)).ToArray());
        CollectionAssert.AreEqual(FakeDisc.TicketBytes(), File.ReadAllBytes(Path.Combine(title.Code, WiiRomInjector.TicketFileName)));
        CollectionAssert.AreEqual(FakeDisc.Tmd(), File.ReadAllBytes(Path.Combine(title.Code, WiiRomInjector.TmdFileName)));
        Assert.IsFalse(File.Exists(Path.Combine(title.Code, "rvlt.old")));
        CollectionAssert.AreEqual(new byte[] { 0x20, 0x00, 0x23, 0xA2 }, File.ReadAllBytes(Path.Combine(title.Code, WiiRomInjector.FirmwareFileName)).Skip(0x100).Take(4).ToArray());
        Assert.AreEqual("52414243", WiiUSharp.MetaXml.Load(title.MetaXmlPath).Get("reserved_flag2"));
        CollectionAssert.Contains(messages, "Decrypting disc");
        CollectionAssert.Contains(messages, "Writing NFS container");
    }

    [TestMethod]
    public async Task InjectAsync_Iso_PayloadRoundTripsThroughNfs()
    {
        var title = StageBase();

        await new WiiRomInjector(FakeDisc.CommonKey).InjectAsync(Injection(), title);

        using var payload = NfsReader.Open(title.Content, NfsKey).OpenPayload();
        var header = new byte[DiscHeader.Size];
        payload.Position = 0;
        ReadExactly(payload, header);
        Assert.AreEqual(FakeDisc.GameId, DiscHeader.Parse(header).GameId);
        Assert.AreEqual(DiscRegion.Japan, RegionArea.Read(payload).Region);
    }

    [TestMethod]
    public async Task InjectAsync_TargetRegion_RewritesRegionAreaInPayload()
    {
        var title = StageBase();
        var injection = Injection(new WiiOptions { TargetRegion = Region.UnitedStates, TrimDisc = false });

        await new WiiRomInjector(FakeDisc.CommonKey).InjectAsync(injection, title);

        using var payload = NfsReader.Open(title.Content, NfsKey).OpenPayload();
        Assert.AreEqual(DiscRegion.UnitedStates, RegionArea.Read(payload).Region);
    }

    [TestMethod]
    public async Task InjectAsync_Wbfs_ProducesTheSameNfsAsTheIso()
    {
        var fromIso = StageBase();
        var fromWbfs = StageBase();
        var wbfs = FakeWbfs.Write(Path.Combine(_root, "wbfs"), FakeWbfs.Build(FakeDisc.Build()), splitAt: (1L << FakeWbfs.WbfsShift) + 777);
        var injector = new WiiRomInjector(FakeDisc.CommonKey);

        await injector.InjectAsync(Injection(), fromIso);
        await injector.InjectAsync(new Injection(Base(), new Rom(wbfs, SourceConsole.Wii), Game()) { Options = new WiiOptions { TrimDisc = false } }, fromWbfs);

        CollectionAssert.AreEqual(
            File.ReadAllBytes(Path.Combine(fromIso.Content, "hif_000000.nfs")),
            File.ReadAllBytes(Path.Combine(fromWbfs.Content, "hif_000000.nfs")));
        CollectionAssert.AreEqual(FakeDisc.Tmd(), File.ReadAllBytes(Path.Combine(fromWbfs.Code, WiiRomInjector.TmdFileName)));
        Assert.AreEqual("52414243", WiiUSharp.MetaXml.Load(fromWbfs.MetaXmlPath).Get("reserved_flag2"));
    }

    [TestMethod]
    public async Task InjectAsync_UnknownExtension_ThrowsNotSupportedException()
    {
        var title = StageBase();
        var injection = new Injection(Base(), new Rom(Path.Combine(_root, "game.gcz"), SourceConsole.Wii), Game());

        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => new WiiRomInjector(FakeDisc.CommonKey).InjectAsync(injection, title));
    }

    [TestMethod]
    public async Task InjectAsync_Nkit_RebuildsFromTheBarePartitionWithoutDecrypting()
    {
        var title = StageBase();
        var nkit = Write("retail.nkit.iso", FakeRetailDisc.Nkit(FakeRetailDisc.Dol(), DiscRegion.Japan));
        var messages = new List<string>();

        await new WiiRomInjector(FakeDisc.CommonKey).InjectAsync(new Injection(Base(), new Rom(nkit, SourceConsole.Wii), Game()) { Options = new WiiOptions { TargetRegion = Region.Europe } }, title, new SyncProgress(messages.Add));

        using var payload = NfsReader.Open(title.Content, NfsKey).OpenPayload();
        var disc = WiiDisc.Read(payload);
        Assert.AreEqual(FakeRetailDisc.GameId, disc.Header.GameId);
        var partition = disc.DataPartitions[0];
        Assert.AreEqual(DiscFormat.RetailDataPartitionOffset, partition.Offset);
        Assert.AreEqual(DiscRegion.Europe, RegionArea.Read(payload).Region, "region option applies to the rebuilt disc");
        CollectionAssert.AreEqual(partition.Ticket.ToBytes(), File.ReadAllBytes(Path.Combine(title.Code, WiiRomInjector.TicketFileName)));
        var boot = PartitionSystemFiles.Read(payload, partition).Boot;
        Assert.IsFalse(NkitHeader.IsPresent(boot), "the rebuilt partition is a plain disc again");
        var data = new PartitionDataStream(payload, partition);
        CollectionAssert.AreEqual(FakeRetailDisc.Dol(), ReadAt(data, Offset(boot, 0x420), 0x800));
        var files = Fst.Parse(ReadAt(data, Offset(boot, 0x424), (int)Offset(boot, 0x428)));
        CollectionAssert.AreEqual(FakeRetailDisc.Files.Select(f => f.Path).ToArray(), files.Select(f => f.Path).ToArray());
        for (var i = 0; i < files.Count; i++)
            CollectionAssert.AreEqual(FakeRetailDisc.Files[i].Content, ReadAt(data, files[i].Offset, (int)files[i].Length), files[i].Path);
        CollectionAssert.AreEqual(new[] { "Expanding NKit image", "Region set to Europe", "Writing NFS container", "Patching fw.img" }, messages);
        CollectionAssert.AreEqual(new[] { "hif_000000.nfs" }, Directory.GetFiles(title.Content).Select(f => Path.GetFileName(f)).ToArray());
    }

    [TestMethod]
    public async Task InjectAsync_NoCommonKey_ServesNkitButNotEncryptedDiscs()
    {
        var title = StageBase();
        var nkit = Write("retail.nkit.iso", FakeRetailDisc.Nkit(FakeRetailDisc.Dol()));

        await new WiiRomInjector(null).InjectAsync(new Injection(Base(), new Rom(nkit, SourceConsole.Wii), Game()), title);
        Assert.IsTrue(File.Exists(Path.Combine(title.Content, "hif_000000.nfs")));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => new WiiRomInjector(null).InjectAsync(Injection(), title));
    }

    [TestMethod]
    public void NeedsCommonKey_OnlyForEncryptedDiscs()
    {
        var nkit = Write("retail.nkit.iso", FakeRetailDisc.Nkit(FakeRetailDisc.Dol()));
        var encrypted = Write("retail.iso", FakeRetailDisc.Encrypted(FakeRetailDisc.Dol()));

        Assert.IsFalse(WiiRomInjector.NeedsCommonKey(nkit));
        Assert.IsTrue(WiiRomInjector.NeedsCommonKey(encrypted));
        Assert.IsTrue(WiiRomInjector.NeedsCommonKey(Path.Combine(_root, "missing.iso")), "an image that cannot be read is assumed encrypted");
        Assert.IsTrue(WiiRomInjector.NeedsCommonKey(Path.Combine(_root, "game.wbfs")));
        Assert.IsFalse(WiiRomInjector.NeedsCommonKey(Path.Combine(_root, "boot.dol")));
        Assert.IsFalse(WiiRomInjector.NeedsCommonKey(Path.Combine(_root, "channel.wad")));
        Assert.ThrowsExactly<ArgumentNullException>(() => WiiRomInjector.NeedsCommonKey(null!));
    }

    [TestMethod]
    public void OpenImage_NkitName_OpensLikeAnyIso()
    {
        var nkit = Write("game.nkit.iso", FakeRetailDisc.Nkit(FakeRetailDisc.Dol()));

        using var container = WiiRomInjector.OpenImage(nkit, out var image);
        using (image)
        {
            Assert.IsTrue(WiiDiscRebuilder.IsNkit(image));
        }
    }

    [TestMethod]
    public void OpenImage_Wbfs_DisposingReleasesTheFile()
    {
        var wbfs = FakeWbfs.Write(Path.Combine(_root, "wbfs"), FakeWbfs.Build(FakeDisc.Build()));

        var container = WiiRomInjector.OpenImage(wbfs, out var image);
        Assert.IsTrue(image.CanSeek);
        image.Dispose();
        container.Dispose();

        File.Delete(wbfs);
    }

    [TestMethod]
    public async Task InjectAsync_NonWiiInjection_ThrowsArgumentException()
    {
        var title = StageBase();
        var @base = new BaseTitle(new TitleId(TitleType.Game, 0x10101D00), "GC Base", Region.UnitedStates, SourceConsole.GameCube);
        var injection = new Injection(@base, new Rom(IsoPath(), SourceConsole.GameCube), Game());

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => new WiiRomInjector(FakeDisc.CommonKey).InjectAsync(injection, title));
    }

    [TestMethod]
    public async Task InjectAsync_NullArguments_ThrowsArgumentNullException()
    {
        var injector = new WiiRomInjector(FakeDisc.CommonKey);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => injector.InjectAsync(null!, StageBase()));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => injector.InjectAsync(Injection(), null!));
    }

    [TestMethod]
    public async Task InjectAsync_UnsupportedOption_ThrowsNotSupportedException()
    {
        var injector = new WiiRomInjector(FakeDisc.CommonKey);

        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => injector.InjectAsync(Injection(new WiiOptions { CheatCodesPath = "codes.gct" }), StageBase()));
    }

    [TestMethod]
    public async Task InjectAsync_Trim_RebuildsTheDiscAndStoresItsNewTicketAndTmd()
    {
        var title = StageBase();
        var iso = Write("retail.iso", FakeRetailDisc.Encrypted(FakeRetailDisc.Dol(), DiscRegion.Europe));
        var messages = new List<string>();

        await new WiiRomInjector(FakeDisc.CommonKey).InjectAsync(new Injection(Base(), new Rom(iso, SourceConsole.Wii), Game()), title, new SyncProgress(messages.Add));

        CollectionAssert.AreEqual(new[] { "hif_000000.nfs" }, Directory.GetFiles(title.Content).Select(f => Path.GetFileName(f)).ToArray());
        using var payload = NfsReader.Open(title.Content, NfsKey).OpenPayload();
        var disc = WiiDisc.Read(payload);
        Assert.AreEqual(FakeRetailDisc.GameId, disc.Header.GameId);
        var partition = disc.DataPartitions[0];
        Assert.AreEqual(DiscFormat.RetailDataPartitionOffset, partition.Offset, "re-laid at the retail offset");
        Assert.AreEqual(DiscRegion.Europe, RegionArea.Read(payload).Region);
        CollectionAssert.AreEqual(partition.Ticket.ToBytes(), File.ReadAllBytes(Path.Combine(title.Code, WiiRomInjector.TicketFileName)));
        CollectionAssert.AreEqual(ReadAt(payload, partition.Offset + partition.Header.TmdOffset, (int)partition.Header.TmdSize), File.ReadAllBytes(Path.Combine(title.Code, WiiRomInjector.TmdFileName)));
        var boot = PartitionSystemFiles.Read(payload, partition).Boot;
        var data = new PartitionDataStream(payload, partition);
        CollectionAssert.AreEqual(FakeRetailDisc.Dol(), ReadAt(data, Offset(boot, 0x420), 0x800), "untouched without patches");
        var files = Fst.Parse(ReadAt(data, Offset(boot, 0x424), (int)Offset(boot, 0x428)));
        CollectionAssert.AreEqual(FakeRetailDisc.Files.Select(f => f.Path).ToArray(), files.Select(f => f.Path).ToArray());
        CollectionAssert.AreEqual(FakeRetailDisc.Files[1].Content, ReadAt(data, files[1].Offset, (int)files[1].Length));
        Assert.AreEqual("52535045", WiiUSharp.MetaXml.Load(title.MetaXmlPath).Get("reserved_flag2"));
        CollectionAssert.AreEqual(new[] { "Decrypting disc", "Rebuilding disc", "Writing NFS container", "Patching fw.img" }, messages);
    }

    [TestMethod]
    public async Task InjectAsync_DolPatches_RewriteMainDolEvenWithoutTrim()
    {
        var title = StageBase();
        var iso = Write("retail.iso", FakeRetailDisc.Encrypted(FakeRetailDisc.Dol()));
        var options = new WiiOptions { TrimDisc = false, RemoveDeflicker = true, RemoveDithering = true, HalfVerticalFilter = true, VideoMode = WiiVideoMode.Pal50 };
        var messages = new List<string>();

        await new WiiRomInjector(FakeDisc.CommonKey).InjectAsync(new Injection(Base(), new Rom(iso, SourceConsole.Wii), Game()) { Options = options }, title, new SyncProgress(messages.Add));

        using var payload = NfsReader.Open(title.Content, NfsKey).OpenPayload();
        var partition = WiiDisc.Read(payload).DataPartitions[0];
        var boot = PartitionSystemFiles.Read(payload, partition).Boot;
        var dol = ReadAt(new PartitionDataStream(payload, partition), Offset(boot, 0x420), 0x800);
        CollectionAssert.AreEqual(WiiRomInjector.PatchMainDol(FakeRetailDisc.Dol(), options), dol, "same result with or without a reporter");
        CollectionAssert.AreEqual(FakeRetailDisc.Pal528IntDfHeader, dol.Skip(FakeRetailDisc.RenderModeOffset).Take(24).ToArray());
        CollectionAssert.Contains(messages, "Deflicker filter removed");
        CollectionAssert.Contains(messages, "Video modes set to Pal50: 1");
        Assert.IsFalse(File.Exists(Path.Combine(title.Content, "rebuilt.iso")));
        Assert.IsFalse(File.Exists(Path.Combine(title.Content, "game.iso")));
    }

    private static BaseTitle Base() =>
        new(new TitleId(TitleType.Game, 0x10101D00), "Wii Base", Region.UnitedStates, SourceConsole.Wii);

    private static Game Game() =>
        new(new TitleId(TitleType.Demo, 0x1ABCDE00), GroupId.Parse("00001ABC"), ProductCode.Parse("WUP-N-TEST"));

    private Injection Injection(WiiOptions? options = null) =>
        new(Base(), new Rom(IsoPath(), SourceConsole.Wii), Game()) { Options = options ?? new WiiOptions { TrimDisc = false } };

    private string IsoPath()
    {
        var path = Path.Combine(_root, "game.iso");
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(_root);
            File.WriteAllBytes(path, FakeDisc.Build());
        }
        return path;
    }

    private static long Offset(byte[] boot, int at) =>
        (long)(uint)(boot[at] << 24 | boot[at + 1] << 16 | boot[at + 2] << 8 | boot[at + 3]) << 2;

    private static byte[] ReadAt(Stream stream, long position, int count)
    {
        var bytes = new byte[count];
        stream.Position = position;
        ReadExactly(stream, bytes);
        return bytes;
    }

    private static void ReadExactly(Stream stream, byte[] buffer)
    {
        var read = 0;
        while (read < buffer.Length)
        {
            var n = stream.Read(buffer, read, buffer.Length - read);
            if (n == 0)
                throw new EndOfStreamException();
            read += n;
        }
    }

    private string Write(string name, byte[] bytes)
    {
        Directory.CreateDirectory(_root);
        var path = Path.Combine(_root, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private TitleDirectory StageBase()
    {
        var title = TitleDirectory.Create(Path.Combine(_root, "title", Guid.NewGuid().ToString("N")));
        File.WriteAllBytes(Path.Combine(title.Code, WiiRomInjector.NfsKeyFileName), NfsKey.ToArray());
        File.WriteAllBytes(Path.Combine(title.Code, "rvlt.old"), new byte[4]);
        var firmware = Enumerable.Repeat((byte)0xEE, 0x1000).ToArray();
        new byte[] { 0x20, 0x07, 0x23, 0xA2 }.CopyTo(firmware, 0x100);
        File.WriteAllBytes(Path.Combine(title.Code, WiiRomInjector.FirmwareFileName), firmware);
        File.WriteAllBytes(Path.Combine(title.Content, "hif_000000.nfs"), new byte[16]);
        File.WriteAllBytes(Path.Combine(title.Content, "hif_000001.nfs"), new byte[16]);
        File.WriteAllText(title.MetaXmlPath, MetaXml);
        return title;
    }

    private sealed class SyncProgress : IProgress<string>
    {
        private readonly Action<string> _report;

        public SyncProgress(Action<string> report)
        {
            _report = report;
        }

        public void Report(string value) => _report(value);
    }
}
