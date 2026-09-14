using PD.WiiU.VirtualConsole.Options;
using WiiSharp;
using WiiUSharp;
using WiiUSharp.Nfs;

namespace PD.WiiU.VirtualConsole.Wii.Tests;

[TestClass]
public class GameCubeRomInjectorTests
{
    private const string MetaXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><menu type=\"complex\" access=\"777\"><reserved_flag2 type=\"hexBinary\" length=\"4\">00000000</reserved_flag2><drc_use type=\"unsignedInt\" length=\"4\">0</drc_use></menu>";
    private static readonly NfsKey NfsKey = new(Enumerable.Range(1, 16).Select(i => (byte)(i * 5)).ToArray());

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
    public void Console_Always_IsGameCube()
    {
        Assert.AreEqual(SourceConsole.GameCube, new GameCubeRomInjector().Console);
    }

    [TestMethod]
    public async Task InjectAsync_Iso_BuildsCarrierFromBaseSystemFilesAndWritesTitleFiles()
    {
        var title = StageBase(DiscRegion.Europe);
        var image = FakeGameCube.Image();
        var iso = Write("game.iso", image);
        var messages = new List<string>();

        await new GameCubeRomInjector().InjectAsync(Injection(iso), title, new SyncProgress(messages.Add));

        CollectionAssert.AreEqual(new[] { "hif_000000.nfs" }, Directory.GetFiles(title.Content).Select(f => Path.GetFileName(f)).ToArray());
        using var payload = NfsReader.Open(title.Content, NfsKey).OpenPayload();
        var disc = WiiDisc.Read(payload);
        Assert.AreEqual(FakeGameCube.GameId, disc.Header.GameId);
        Assert.AreEqual(FakeGameCube.Title, disc.Header.Title);
        Assert.AreEqual(DiscRegion.Europe, RegionArea.Read(payload).Region, "region follows the base");
        var partition = disc.DataPartitions[0];
        Assert.AreEqual(DiscFormat.RetailDataPartitionOffset, partition.Offset);

        var expected = FakeGameCube.BaseSystem();
        var system = PartitionSystemFiles.Read(payload, partition);
        CollectionAssert.AreEqual(expected.Apploader.ToBytes(), system.Apploader.ToBytes());
        CollectionAssert.AreEqual(expected.Bi2, system.Bi2);
        CollectionAssert.AreEqual(expected.CertificateChain, system.CertificateChain);
        CollectionAssert.AreEqual(expected.Boot.Skip(0x430).Take(8).ToArray(), system.Boot.Skip(0x430).Take(8).ToArray());
        Assert.AreEqual(FakeGameCube.GameId, System.Text.Encoding.ASCII.GetString(system.Boot, 0, 6));

        var data = new PartitionDataStream(payload, partition);
        var forwarder = NintendontForwarder.Embedded();
        CollectionAssert.AreEqual(forwarder, ReadAt(data, ReadOffset(system.Boot, 0x420), forwarder.Length));
        var files = Fst.Parse(ReadAt(data, ReadOffset(system.Boot, 0x424), (int)ReadOffset(system.Boot, 0x428)));
        Assert.AreEqual(1, files.Count);
        Assert.AreEqual(GameCubeRomInjector.GameFileName, files[0].Path);
        Assert.AreEqual(image.Length, files[0].Length);
        CollectionAssert.AreEqual(image, ReadAt(data, files[0].Offset, image.Length));

        CollectionAssert.AreEqual(partition.Ticket.ToBytes(), File.ReadAllBytes(Path.Combine(title.Code, WiiRomInjector.TicketFileName)));
        var tmd = File.ReadAllBytes(Path.Combine(title.Code, WiiRomInjector.TmdFileName));
        CollectionAssert.AreEqual(ReadAt(payload, partition.Offset + partition.Header.TmdOffset, (int)partition.Header.TmdSize), tmd);
        CollectionAssert.AreEqual(new byte[] { 0, 1, 0, 0, 0x47, 0x41, 0x4C, 0x45 }, Tmd.Parse(tmd).TitleId);
        Assert.IsFalse(File.Exists(Path.Combine(title.Code, "rvlt.old")));

        var firmware = File.ReadAllBytes(Path.Combine(title.Code, WiiRomInjector.FirmwareFileName));
        CollectionAssert.AreEqual(new byte[] { 0x20, 0x00, 0x23, 0xA2 }, firmware.Skip(0x100).Take(4).ToArray(), "fakesign");
        CollectionAssert.AreEqual(new byte[] { 0x46, 0xC0 }, firmware.Skip(0x200).Take(2).ToArray(), "homebrew");

        var meta = WiiUSharp.MetaXml.Load(title.MetaXmlPath);
        Assert.AreEqual("47414c45", meta.Get("reserved_flag2"));
        Assert.AreEqual("65537", meta.Get("drc_use"));
        CollectionAssert.AreEqual(new[] { "Reading base disc", "Building carrier disc", "Writing NFS container", "Patching fw.img" }, messages);
    }

    [TestMethod]
    public async Task InjectAsync_GczWithSecondDiscAndForcedAspect_PlacesBothDiscsBehindTheForcedForwarder()
    {
        var title = StageBase(DiscRegion.UnitedStates);
        var first = FakeGameCube.Image(0x1801, seed: 1);
        var second = FakeGameCube.Image(0x0777, seed: 2);
        var gcz = Write("game.gcz", FakeGameCube.Gcz(first));
        var disc2 = Write("disc2.gcm", second);
        var injection = new Injection(Base(), new Rom(gcz, SourceConsole.GameCube), Game())
        {
            Options = new GameCubeOptions { SecondDiscPath = disc2, ForceFourByThree = true },
        };

        await new GameCubeRomInjector().InjectAsync(injection, title);

        using var payload = NfsReader.Open(title.Content, NfsKey).OpenPayload();
        var partition = WiiDisc.Read(payload).DataPartitions[0];
        var boot = PartitionSystemFiles.Read(payload, partition).Boot;
        var data = new PartitionDataStream(payload, partition);
        var forwarder = NintendontForwarder.Embedded(forceFourByThree: true);
        CollectionAssert.AreEqual(forwarder, ReadAt(data, ReadOffset(boot, 0x420), forwarder.Length));
        var files = Fst.Parse(ReadAt(data, ReadOffset(boot, 0x424), (int)ReadOffset(boot, 0x428)));
        CollectionAssert.AreEqual(new[] { GameCubeRomInjector.GameFileName, GameCubeRomInjector.SecondDiscFileName }, files.Select(f => f.Path).ToArray());
        CollectionAssert.AreEqual(first, ReadAt(data, files[0].Offset, first.Length));
        CollectionAssert.AreEqual(second, ReadAt(data, files[1].Offset, second.Length));
        Assert.AreEqual(DiscRegion.UnitedStates, RegionArea.Read(payload).Region);
    }

    [TestMethod]
    public async Task InjectAsync_ForwarderPath_UsesThatFileAsMainDol()
    {
        var title = StageBase(DiscRegion.Japan);
        var custom = Write("custom.dol", Enumerable.Range(0, 300).Select(i => (byte)(i ^ 0x33)).ToArray());
        var injection = new Injection(Base(), new Rom(Write("game.iso", FakeGameCube.Image()), SourceConsole.GameCube), Game())
        {
            Options = new GameCubeOptions { ForwarderPath = custom },
        };

        await new GameCubeRomInjector().InjectAsync(injection, title);

        using var payload = NfsReader.Open(title.Content, NfsKey).OpenPayload();
        var partition = WiiDisc.Read(payload).DataPartitions[0];
        var boot = PartitionSystemFiles.Read(payload, partition).Boot;
        CollectionAssert.AreEqual(File.ReadAllBytes(custom), ReadAt(new PartitionDataStream(payload, partition), ReadOffset(boot, 0x420), 300));
    }

    [TestMethod]
    public async Task InjectAsync_NotAGameCubeImage_ThrowsInvalidDataException()
    {
        var title = StageBase(DiscRegion.UnitedStates);
        var wii = Write("wii.iso", FakeGameCube.BaseDisc(DiscRegion.UnitedStates));

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => new GameCubeRomInjector().InjectAsync(Injection(wii), title));
        Assert.IsFalse(File.Exists(Path.Combine(title.Content, "carrier.iso")));
    }

    [TestMethod]
    public async Task InjectAsync_UnsupportedImageName_ThrowsNotSupportedException()
    {
        var title = StageBase(DiscRegion.UnitedStates);

        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => new GameCubeRomInjector().InjectAsync(Injection(Path.Combine(_root, "game.nkit.iso")), title));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => new GameCubeRomInjector().InjectAsync(Injection(Path.Combine(_root, "game.wbfs")), title));
    }

    [TestMethod]
    public async Task InjectAsync_WrongConsoleOrNulls_Throw()
    {
        var title = StageBase(DiscRegion.UnitedStates);
        var wii = new Injection(new BaseTitle(new TitleId(TitleType.Game, 1), "Wii", Region.UnitedStates, SourceConsole.Wii), new Rom("x.iso", SourceConsole.Wii), Game());

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => new GameCubeRomInjector().InjectAsync(wii, title));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => new GameCubeRomInjector().InjectAsync(null!, title));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => new GameCubeRomInjector().InjectAsync(Injection("x.iso"), null!));
    }

    [TestMethod]
    public void GameCubeImage_Gcz_DecodesAndDisposingReleasesTheFile()
    {
        var plain = FakeGameCube.Image(0x1234);
        var gcz = Write("game.gcz", FakeGameCube.Gcz(plain));

        var container = GameCubeImage.Open(gcz, out var image);
        Assert.AreEqual(FakeGameCube.GameId, GameCubeImage.ReadHeader(image).GameId);
        CollectionAssert.AreEqual(plain, ReadAt(image, 0, plain.Length));
        image.Dispose();
        container.Dispose();

        File.Delete(gcz);
        Assert.ThrowsExactly<ArgumentNullException>(() => GameCubeImage.Open(null!, out _));
        Assert.ThrowsExactly<ArgumentNullException>(() => GameCubeImage.ReadHeader(null!));
        Assert.ThrowsExactly<InvalidDataException>(() => GameCubeImage.ReadHeader(new MemoryStream(new byte[8])));
    }

    [TestMethod]
    public void NintendontForwarder_Embedded_AreTheFix94Builds()
    {
        var normal = NintendontForwarder.Embedded();
        var forced = NintendontForwarder.Embedded(forceFourByThree: true);

        Assert.AreEqual(198336, normal.Length);
        Assert.AreEqual(198336, forced.Length);
        Assert.IsFalse(Enumerable.SequenceEqual(normal, forced));
        Assert.AreEqual(0x100u, (uint)(normal[0] << 24 | normal[1] << 16 | normal[2] << 8 | normal[3]), "first text section follows the DOL header");
    }

    [TestMethod]
    public void VWiiMeta_EncodeGameCode_HexOfFirstFourCharacters()
    {
        Assert.AreEqual("47414c45", VWiiMeta.EncodeGameCode("GALE01"));
        Assert.ThrowsExactly<ArgumentException>(() => VWiiMeta.EncodeGameCode("GA"));
        Assert.ThrowsExactly<ArgumentNullException>(() => VWiiMeta.EncodeGameCode(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => VWiiMeta.Apply(null!, "GALE01"));
    }

    private static BaseTitle Base() =>
        new(new TitleId(TitleType.Game, 0x10101D00), "GC Base", Region.UnitedStates, SourceConsole.GameCube);

    private static Game Game() =>
        new(new TitleId(TitleType.Demo, 0x1ABCDE00), GroupId.Parse("00001ABC"), ProductCode.Parse("WUP-N-TEST"));

    private static Injection Injection(string rom) =>
        new(Base(), new Rom(rom, SourceConsole.GameCube), Game());

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

    private static long ReadOffset(byte[] boot, int at) =>
        (long)(uint)(boot[at] << 24 | boot[at + 1] << 16 | boot[at + 2] << 8 | boot[at + 3]) << 2;

    private TitleDirectory StageBase(DiscRegion region)
    {
        var title = TitleDirectory.Create(Path.Combine(_root, "title", Guid.NewGuid().ToString("N")));
        File.WriteAllBytes(Path.Combine(title.Code, WiiRomInjector.NfsKeyFileName), NfsKey.ToArray());
        File.WriteAllBytes(Path.Combine(title.Code, "rvlt.old"), new byte[4]);
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
