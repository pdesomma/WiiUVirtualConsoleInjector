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
        var injection = Injection(new WiiOptions { TargetRegion = Region.UnitedStates });

        await new WiiRomInjector(FakeDisc.CommonKey).InjectAsync(injection, title);

        using var payload = NfsReader.Open(title.Content, NfsKey).OpenPayload();
        Assert.AreEqual(DiscRegion.UnitedStates, RegionArea.Read(payload).Region);
    }

    [TestMethod]
    public async Task InjectAsync_NonIsoRom_ThrowsNotSupportedException()
    {
        var title = StageBase();
        var injection = new Injection(Base(), new Rom(Path.Combine(_root, "game.wbfs"), SourceConsole.Wii), Game());

        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => new WiiRomInjector(FakeDisc.CommonKey).InjectAsync(injection, title));
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

        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => injector.InjectAsync(Injection(new WiiOptions { RemoveDeflicker = true }), StageBase()));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => injector.InjectAsync(Injection(new WiiOptions { TrimDisc = false }), StageBase()));
        await Assert.ThrowsExactlyAsync<NotSupportedException>(() => injector.InjectAsync(Injection(new WiiOptions { CheatCodesPath = "codes.gct" }), StageBase()));
    }

    private static BaseTitle Base() =>
        new(new TitleId(TitleType.Game, 0x10101D00), "Wii Base", Region.UnitedStates, SourceConsole.Wii);

    private static Game Game() =>
        new(new TitleId(TitleType.Demo, 0x1ABCDE00), GroupId.Parse("00001ABC"), ProductCode.Parse("WUP-N-TEST"));

    private Injection Injection(WiiOptions? options = null) =>
        new(Base(), new Rom(IsoPath(), SourceConsole.Wii), Game()) { Options = options };

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
