using PD.WiiU.VirtualConsole.Options;
using WiiUSharp;
using WiiUSharp.Rpx;

namespace PD.WiiU.VirtualConsole.Retro.Tests;

[TestClass]
public class RetroInjectorTests
{
    private string _root = null!;

    [TestInitialize]
    public void Initialize() => _root = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Retro.Tests", Guid.NewGuid().ToString("N"));

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void Console_Injectors_ReportTheirConsole()
    {
        Assert.AreEqual(SourceConsole.Nes, new NesRomInjector().Console);
        Assert.AreEqual(SourceConsole.Snes, new SnesRomInjector().Console);
    }

    [TestMethod]
    public async Task InjectAsync_NesRomIntoCompressedBase_ReplacesRomAndRecompresses()
    {
        var title = StageBase(nes: true);
        var rom = WriteRom("game.nes", FakeVcRpx.NesRom(0x6010));
        var messages = new List<string>();

        await new NesRomInjector().InjectAsync(Injection(SourceConsole.Nes, rom), title, new SyncProgress(messages.Add));

        var rpx = RpxFile.Load(Path.Combine(title.Code, "WUP-JAAE.rpx"));
        Assert.IsTrue(rpx.FindSection(".rodata")!.StoredCompressed, "saved compressed");
        var slot = RomSlot.Find(rpx);
        CollectionAssert.AreEqual(FakeVcRpx.NesRom(0x6010), slot.Section.Data.Skip(slot.Offset).Take(0x6010).ToArray());
        CollectionAssert.AreEqual(FakeVcRpx.TvPattern, rpx.FindSection(".text")!.Data.Skip(0x100).Take(7).ToArray(), "no display patch by default");
        Assert.AreEqual(3, messages.Count);
    }

    [TestMethod]
    public async Task InjectAsync_SnesPixelPerfect_PatchesDisplay()
    {
        var title = StageBase(nes: false);
        var rom = WriteRom("game.sfc", FakeVcRpx.SnesRom(0x10000));
        var injection = new Injection(Base(SourceConsole.Snes), new Rom(rom, SourceConsole.Snes), Game()) { Options = new SnesOptions { PixelPerfect = true } };

        await new SnesRomInjector().InjectAsync(injection, title);

        var rpx = RpxFile.Load(Path.Combine(title.Code, "WUP-JAAE.rpx"));
        CollectionAssert.AreEqual(new byte[] { 0x04, 0x38, 0x38, 0xE0, 0x08, 0xC0, 0x90 }, rpx.FindSection(".text")!.Data.Skip(0x100).Take(7).ToArray());
        var slot = RomSlot.Find(rpx);
        CollectionAssert.AreEqual(FakeVcRpx.SnesRom(0x10000), slot.Section.Data.Skip(slot.Offset).Take(0x10000).ToArray());
    }

    [TestMethod]
    public async Task InjectAsync_SnesRomWithCopierHeader_InjectsWithoutIt()
    {
        var title = StageBase(nes: false);
        var bare = FakeVcRpx.SnesRom(0x10000);
        var headered = new byte[SnesCopierHeader.Size + bare.Length];
        System.Text.Encoding.ASCII.GetBytes("GAME DOCTOR SF 3").CopyTo(headered, 0);
        bare.CopyTo(headered, SnesCopierHeader.Size);
        var rom = WriteRom("game.smc", headered);
        var reports = new List<string>();

        await new SnesRomInjector().InjectAsync(Injection(SourceConsole.Snes, rom), title, new Progress<string>(reports.Add));
        await Task.Yield();

        var slot = RomSlot.Find(RpxFile.Load(Path.Combine(title.Code, "WUP-JAAE.rpx")));
        CollectionAssert.AreEqual(bare, slot.Section.Data.Skip(slot.Offset).Take(bare.Length).ToArray(), "the 512 header bytes are gone");
    }

    [TestMethod]
    public async Task InjectAsync_RomTooLarge_ThrowsArgumentException()
    {
        var title = StageBase(nes: false);
        var rom = WriteRom("game.sfc", FakeVcRpx.SnesRom(FakeVcRpx.SlotCapacity + 1));

        var e = await Assert.ThrowsExactlyAsync<ArgumentException>(() => new SnesRomInjector().InjectAsync(Injection(SourceConsole.Snes, rom), title));
        StringAssert.Contains(e.Message, "this base holds 128 KB");
        StringAssert.Contains(e.Message, "Kirby's Dream Land 3");
    }

    [TestMethod]
    public void Capacity_And_RomSize_MeasureSlotAndRom()
    {
        var snes = StageBase(nes: false);
        var nes = StageBase(nes: true);
        var headered = WriteRom("game.smc", new byte[0x20000 + 512]);
        var plain = WriteRom("game.nes", FakeVcRpx.NesRom(0x8010));

        Assert.AreEqual(FakeVcRpx.SlotCapacity, new SnesRomInjector().Capacity(snes));
        Assert.AreEqual(FakeVcRpx.SlotCapacity + 16, new NesRomInjector().Capacity(nes));
        Assert.AreEqual(0x20000, new SnesRomInjector().RomSize(headered), "copier header dropped");
        Assert.AreEqual(0x8010, new NesRomInjector().RomSize(plain));
        Assert.ThrowsExactly<ArgumentNullException>(() => new NesRomInjector().Capacity(null!));
        Assert.ThrowsExactly<ArgumentException>(() => new SnesRomInjector().RomSize(" "));
    }

    [TestMethod]
    public void Kilobytes_RoundsToNearestAndPrefersWholeMegabytes()
    {
        Assert.AreEqual("64 KB", RomSlot.Kilobytes(0x10000));
        Assert.AreEqual("64 KB", RomSlot.Kilobytes(0x10010));
        Assert.AreEqual("384 KB", RomSlot.Kilobytes(393232));
        Assert.AreEqual("1 MB", RomSlot.Kilobytes(0x100010));
        Assert.AreEqual("4 MB", RomSlot.Kilobytes(0x400000));
    }

    [TestMethod]
    public async Task InjectAsync_NesInjectorOnSnesBase_ThrowsInvalidDataException()
    {
        var title = StageBase(nes: false);
        var rom = WriteRom("game.nes", FakeVcRpx.NesRom(0x2010));

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => new NesRomInjector().InjectAsync(Injection(SourceConsole.Nes, rom), title));
    }

    [TestMethod]
    public async Task InjectAsync_NoRpxInBase_ThrowsFileNotFoundException()
    {
        var title = TitleDirectory.Create(Path.Combine(_root, "empty"));
        var rom = WriteRom("game.nes", FakeVcRpx.NesRom(0x2010));

        await Assert.ThrowsExactlyAsync<FileNotFoundException>(() => new NesRomInjector().InjectAsync(Injection(SourceConsole.Nes, rom), title));
    }

    [TestMethod]
    public async Task InjectAsync_WrongConsoleOrNulls_Throw()
    {
        var title = StageBase(nes: true);
        var rom = WriteRom("game.nes", FakeVcRpx.NesRom(0x2010));

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => new SnesRomInjector().InjectAsync(Injection(SourceConsole.Nes, rom), title));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => new NesRomInjector().InjectAsync(null!, title));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => new NesRomInjector().InjectAsync(Injection(SourceConsole.Nes, rom), null!));
    }

    private static BaseTitle Base(SourceConsole console) =>
        new(new TitleId(TitleType.Game, 0x10101D00), "Base", Region.UnitedStates, console);

    private static Game Game() =>
        new(new TitleId(TitleType.Demo, 0x1ABCDE00), GroupId.Parse("00001ABC"), ProductCode.Parse("WUP-N-TEST"));

    private static Injection Injection(SourceConsole console, string rom) =>
        new(Base(console), new Rom(rom, console), Game());

    private TitleDirectory StageBase(bool nes)
    {
        var title = TitleDirectory.Create(Path.Combine(_root, "title", Guid.NewGuid().ToString("N")));
        RpxFile.Parse(FakeVcRpx.Build(nes)).Save(Path.Combine(title.Code, "WUP-JAAE.rpx"), compress: true);
        return title;
    }

    private string WriteRom(string name, byte[] bytes)
    {
        Directory.CreateDirectory(_root);
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
