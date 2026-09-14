using PD.WiiU.VirtualConsole.Options;
using WiiSharp;
using WiiUSharp;
using WiiUSharp.Nfs;

namespace PD.WiiU.VirtualConsole.Wii.Tests;

[TestClass]
public class HomebrewInjectTests
{
    private const string MetaXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><menu type=\"complex\" access=\"777\"><reserved_flag2 type=\"hexBinary\" length=\"4\">00000000</reserved_flag2><drc_use type=\"unsignedInt\" length=\"4\">0</drc_use></menu>";
    private static readonly NfsKey NfsKey = new(Enumerable.Range(1, 16).Select(i => (byte)(i * 5)).ToArray());
    private static readonly byte[] ChannelTitleId = { 0x00, 0x01, 0x00, 0x01, 0x57, 0x41, 0x4C, 0x50 };

    private string _root = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Wii.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_root, recursive: true);

    [TestMethod]
    public async Task InjectAsync_Dol_BecomesMainDolOfAnEmptyCarrier()
    {
        var title = StageBase(DiscRegion.Japan);
        var dol = Write("My App.dol", FakeRetailDisc.Dol());
        var messages = new List<string>();

        await new WiiRomInjector(FakeDisc.CommonKey).InjectAsync(Injection(dol), title, new SyncProgress(messages.Add));

        using var payload = NfsReader.Open(title.Content, NfsKey).OpenPayload();
        var disc = WiiDisc.Read(payload);
        Assert.AreEqual(WiiRomInjector.HomebrewGameId, disc.Header.GameId);
        Assert.AreEqual("My App", disc.Header.Title);
        Assert.AreEqual(DiscRegion.Japan, RegionArea.Read(payload).Region);
        var partition = disc.DataPartitions[0];
        var boot = PartitionSystemFiles.Read(payload, partition).Boot;
        var data = new PartitionDataStream(payload, partition);
        CollectionAssert.AreEqual(FakeRetailDisc.Dol(), ReadAt(data, Offset(boot, 0x420), 0x800));
        Assert.AreEqual(0, Fst.Parse(ReadAt(data, Offset(boot, 0x424), (int)Offset(boot, 0x428))).Count);
        CollectionAssert.AreEqual(partition.Ticket.ToBytes(), File.ReadAllBytes(Path.Combine(title.Code, WiiRomInjector.TicketFileName)));
        var firmware = File.ReadAllBytes(Path.Combine(title.Code, WiiRomInjector.FirmwareFileName));
        CollectionAssert.AreEqual(new byte[] { 0x46, 0xC0 }, firmware.Skip(0x200).Take(2).ToArray(), "homebrew patch applied");
        Assert.AreEqual("00000000", WiiUSharp.MetaXml.Load(title.MetaXmlPath).Get("reserved_flag2"), "no game code for homebrew");
        CollectionAssert.AreEqual(new[] { "Reading base disc", "Building carrier disc", "Writing NFS container", "Patching fw.img" }, messages);
    }

    [TestMethod]
    public async Task InjectAsync_Wad_WritesTheBooterAndTitleTxt()
    {
        var title = StageBase(DiscRegion.UnitedStates);
        var wad = Write("channel.wad", FakeWad.Build(ChannelTitleId));
        var messages = new List<string>();

        await new WiiRomInjector(FakeDisc.CommonKey).InjectAsync(Injection(wad), title, new SyncProgress(messages.Add));

        using var payload = NfsReader.Open(title.Content, NfsKey).OpenPayload();
        var disc = WiiDisc.Read(payload);
        Assert.AreEqual("WALP01", disc.Header.GameId);
        Assert.AreEqual("Channel WALP", disc.Header.Title);
        var partition = disc.DataPartitions[0];
        var boot = PartitionSystemFiles.Read(payload, partition).Boot;
        var data = new PartitionDataStream(payload, partition);
        var booter = ChannelBooter.Embedded();
        CollectionAssert.AreEqual(booter, ReadAt(data, Offset(boot, 0x420), booter.Length));
        var files = Fst.Parse(ReadAt(data, Offset(boot, 0x424), (int)Offset(boot, 0x428)));
        Assert.AreEqual(1, files.Count);
        Assert.AreEqual(ChannelBooter.TitleFileName, files[0].Path);
        Assert.AreEqual(4, files[0].Length);
        Assert.AreEqual("WALP", System.Text.Encoding.ASCII.GetString(ReadAt(data, files[0].Offset, 4)));
        CollectionAssert.Contains(messages, "Forwarding to channel WALP");
    }

    [TestMethod]
    public async Task InjectAsync_WadWithForcedAspectAndCustomBooter_UsesTheGivenDol()
    {
        var title = StageBase(DiscRegion.UnitedStates);
        var wad = Write("channel.wad", FakeWad.Build(ChannelTitleId));
        var forced = StageBase(DiscRegion.UnitedStates);
        var custom = Write("booter.dol", Enumerable.Range(0, 200).Select(i => (byte)(i ^ 0x77)).ToArray());

        await new WiiRomInjector(FakeDisc.CommonKey).InjectAsync(Injection(wad, new WiiOptions { ForceFourByThree = true }), forced);
        await new WiiRomInjector(FakeDisc.CommonKey).InjectAsync(Injection(wad, new WiiOptions { ForwarderPath = custom }), title);

        Assert.IsTrue(Enumerable.SequenceEqual(ChannelBooter.Embedded(forceFourByThree: true), MainDol(forced, ChannelBooter.Embedded(forceFourByThree: true).Length)));
        CollectionAssert.AreEqual(File.ReadAllBytes(custom), MainDol(title, 200));
    }

    [TestMethod]
    public async Task InjectAsync_DolWithoutSections_ThrowsInvalidDataException()
    {
        var title = StageBase(DiscRegion.UnitedStates);
        var dol = Write("bad.dol", new byte[0x200]);

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => new WiiRomInjector(FakeDisc.CommonKey).InjectAsync(Injection(dol), title));
    }

    [TestMethod]
    public void ChannelBooter_Embedded_AreTheFix94Builds()
    {
        Assert.AreEqual(167904, ChannelBooter.Embedded().Length);
        Assert.AreEqual(167968, ChannelBooter.Embedded(forceFourByThree: true).Length);
        Assert.AreEqual("WALP01", WiiRomInjector.ChannelGameId(ChannelTitleId));
        Assert.ThrowsExactly<ArgumentException>(() => WiiRomInjector.ChannelGameId(new byte[4]));
        Assert.ThrowsExactly<ArgumentNullException>(() => WiiRomInjector.ChannelGameId(null!));
    }

    [TestMethod]
    public void CarrierDisc_Ascii_ReplacesAndTruncates()
    {
        Assert.AreEqual("Caf? Game", CarrierDisc.Ascii("Café Game"));
        Assert.AreEqual(64, CarrierDisc.Ascii(new string('x', 100)).Length);
        Assert.ThrowsExactly<ArgumentNullException>(() => CarrierDisc.Ascii(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => CarrierDisc.Write(null!, "HBRW01", "t", new byte[1], Array.Empty<DiscFile>()));
    }

    private static BaseTitle Base() =>
        new(new TitleId(TitleType.Game, 0x10101D00), "Wii Base", Region.UnitedStates, SourceConsole.Wii);

    private static Game Game() =>
        new(new TitleId(TitleType.Demo, 0x1ABCDE00), GroupId.Parse("00001ABC"), ProductCode.Parse("WUP-N-TEST"));

    private static Injection Injection(string rom, WiiOptions? options = null) =>
        new(Base(), new Rom(rom, SourceConsole.Wii), Game()) { Options = options };

    private static byte[] MainDol(TitleDirectory title, int length)
    {
        using var payload = NfsReader.Open(title.Content, NfsKey).OpenPayload();
        var partition = WiiDisc.Read(payload).DataPartitions[0];
        var boot = PartitionSystemFiles.Read(payload, partition).Boot;
        return ReadAt(new PartitionDataStream(payload, partition), Offset(boot, 0x420), length);
    }

    private static long Offset(byte[] boot, int at) =>
        (long)(uint)(boot[at] << 24 | boot[at + 1] << 16 | boot[at + 2] << 8 | boot[at + 3]) << 2;

    private static byte[] ReadAt(Stream stream, long position, int count)
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

    private TitleDirectory StageBase(DiscRegion region)
    {
        var title = TitleDirectory.Create(Path.Combine(_root, "title", Guid.NewGuid().ToString("N")));
        File.WriteAllBytes(Path.Combine(title.Code, WiiRomInjector.NfsKeyFileName), NfsKey.ToArray());
        var firmware = Enumerable.Repeat((byte)0xEE, 0x1000).ToArray();
        new byte[] { 0x20, 0x07, 0x23, 0xA2 }.CopyTo(firmware, 0x100);
        new byte[] { 0xD0, 0x0B, 0x23, 0x08, 0x43, 0x13, 0x60, 0x0B }.CopyTo(firmware, 0x200);
        File.WriteAllBytes(Path.Combine(title.Code, WiiRomInjector.FirmwareFileName), firmware);
        var baseDisc = FakeGameCube.BaseDisc(region);
        var partition = WiiDisc.Read(new MemoryStream(baseDisc)).DataPartitions[0];
        new NfsWriter(NfsKey).Write(new MemoryStream(baseDisc), new DiscDataSpan(partition.Offset, partition.DataEnd - partition.Offset), title.Content);
        File.WriteAllText(title.MetaXmlPath, MetaXml);
        return title;
    }

    private string Write(string name, byte[] bytes)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllBytes(path, bytes);
        return path;
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
