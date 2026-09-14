using PD.WiiU.VirtualConsole.Options;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Retro.Tests;

[TestClass]
public class N64Tests
{
    private static readonly byte[] Z64 = { 0x80, 0x37, 0x12, 0x40, 0x00, 0x00, 0x00, 0x0F, 0x80, 0x00, 0x04, 0x00, 0x00, 0x00, 0x14, 0x49 };

    private string _root = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Retro.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_root, recursive: true);

    [TestMethod]
    public void ToBigEndian_Z64_ReturnsCopy()
    {
        var result = N64Rom.ToBigEndian(Z64);

        CollectionAssert.AreEqual(Z64, result);
        Assert.AreNotSame(Z64, result);
    }

    [TestMethod]
    public void ToBigEndian_V64_SwapsEveryPair()
    {
        var v64 = new byte[Z64.Length];
        for (var i = 0; i < Z64.Length; i += 2)
        {
            v64[i] = Z64[i + 1];
            v64[i + 1] = Z64[i];
        }

        CollectionAssert.AreEqual(Z64, N64Rom.ToBigEndian(v64));
    }

    [TestMethod]
    public void ToBigEndian_N64_ReversesEveryWord()
    {
        var n64 = new byte[Z64.Length];
        for (var i = 0; i < Z64.Length; i += 4)
            for (var j = 0; j < 4; j++)
                n64[i + j] = Z64[i + 3 - j];

        CollectionAssert.AreEqual(Z64, N64Rom.ToBigEndian(n64));
    }

    [TestMethod]
    public void ToBigEndian_UnknownHeader_ThrowsInvalidDataException()
    {
        Assert.ThrowsExactly<InvalidDataException>(() => N64Rom.ToBigEndian(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
        Assert.ThrowsExactly<InvalidDataException>(() => N64Rom.ToBigEndian(new byte[2]));
        Assert.ThrowsExactly<ArgumentNullException>(() => N64Rom.ToBigEndian(null!));
    }

    [TestMethod]
    public void FrameLayoutPatch_WideAndRemove_RewritesFramePaneAndHidesMask()
    {
        var archive = FakeFrameLayout.Build();

        FrameLayoutPatch.Apply(archive, wideScreen: true, removeDarkFilter: true);

        Assert.AreEqual(0u, ReadUInt32(archive, FakeFrameLayout.FrameOffset + 0x2C));
        Assert.AreEqual(0u, ReadUInt32(archive, FakeFrameLayout.FrameOffset + 0x30));
        Assert.AreEqual(0x3F800000u, ReadUInt32(archive, FakeFrameLayout.FrameOffset + 0x44));
        Assert.AreEqual(0x3F800000u, ReadUInt32(archive, FakeFrameLayout.FrameOffset + 0x48));
        Assert.AreEqual(0x44F00000u, ReadUInt32(archive, FakeFrameLayout.FrameOffset + 0x4C));
        Assert.AreEqual(0, archive[FakeFrameLayout.MaskOffset + 0x08]);
    }

    [TestMethod]
    public void FrameLayoutPatch_StandardKeepFilter_UsesFourByThreeWidthAndShowsMask()
    {
        var archive = FakeFrameLayout.Build();

        FrameLayoutPatch.Apply(archive, wideScreen: false, removeDarkFilter: false);

        Assert.AreEqual(0x44B40000u, ReadUInt32(archive, FakeFrameLayout.FrameOffset + 0x4C));
        Assert.AreEqual(1, archive[FakeFrameLayout.MaskOffset + 0x08]);
    }

    [TestMethod]
    public void FrameLayoutPatch_NotSarc_ThrowsInvalidDataException()
    {
        var archive = FakeFrameLayout.Build();
        archive[0] = 0;

        Assert.ThrowsExactly<InvalidDataException>(() => FrameLayoutPatch.Apply(archive, true, true));
    }

    [TestMethod]
    public void FrameLayoutPatch_MissingMaskPane_ThrowsInvalidDataException()
    {
        var archive = FakeFrameLayout.Build();
        archive[FakeFrameLayout.MaskOffset + 0x0C + 6] = (byte)'X';

        Assert.ThrowsExactly<InvalidDataException>(() => FrameLayoutPatch.Apply(archive, true, true));
    }

    [TestMethod]
    public async Task InjectAsync_V64WithIniAndWide_WritesRomIniAndLayout()
    {
        var title = StageBase();
        var v64 = Z64.Select((b, i) => Z64[i ^ 1]).ToArray();
        var rom = Write("game.v64", v64);
        var ini = Write("custom.ini", System.Text.Encoding.ASCII.GetBytes("[settings]\n"));
        var injection = new Injection(Base(), new Rom(rom, SourceConsole.N64), Game()) { Options = new N64Options { IniPath = ini, WideScreen = true } };
        var messages = new List<string>();

        await new N64RomInjector().InjectAsync(injection, title, new SyncProgress(messages.Add));

        CollectionAssert.AreEqual(Z64, File.ReadAllBytes(Path.Combine(title.Content, "rom", "NAAE.z64")));
        Assert.AreEqual("[settings]\n", File.ReadAllText(Path.Combine(title.Content, "config", "NAAE.z64.ini")));
        var archive = File.ReadAllBytes(Path.Combine(title.Content, "FrameLayout.arc"));
        Assert.AreEqual(0x44F00000u, ReadUInt32(archive, FakeFrameLayout.FrameOffset + 0x4C));
        Assert.AreEqual(1, archive[FakeFrameLayout.MaskOffset + 0x08], "filter kept");
        Assert.AreEqual(3, messages.Count);
    }

    [TestMethod]
    public async Task InjectAsync_NoOptions_WritesEmptyIniAndLeavesLayout()
    {
        var title = StageBase();
        var rom = Write("game.z64", Z64);
        var before = File.ReadAllBytes(Path.Combine(title.Content, "FrameLayout.arc"));

        await new N64RomInjector().InjectAsync(new Injection(Base(), new Rom(rom, SourceConsole.N64), Game()), title);

        Assert.AreEqual(0, new FileInfo(Path.Combine(title.Content, "config", "NAAE.z64.ini")).Length);
        CollectionAssert.AreEqual(before, File.ReadAllBytes(Path.Combine(title.Content, "FrameLayout.arc")));
    }

    [TestMethod]
    public async Task InjectAsync_NoRomInBase_ThrowsFileNotFoundException()
    {
        var title = TitleDirectory.Create(Path.Combine(_root, "empty"));
        var rom = Write("game.z64", Z64);

        await Assert.ThrowsExactlyAsync<FileNotFoundException>(() => new N64RomInjector().InjectAsync(new Injection(Base(), new Rom(rom, SourceConsole.N64), Game()), title));
    }

    [TestMethod]
    public async Task InjectAsync_WrongConsoleOrNulls_Throw()
    {
        var title = StageBase();
        var nes = new Injection(new BaseTitle(new TitleId(TitleType.Game, 1), "NES", Region.UnitedStates, SourceConsole.Nes), new Rom("x.nes", SourceConsole.Nes), Game());

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => new N64RomInjector().InjectAsync(nes, title));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => new N64RomInjector().InjectAsync(null!, title));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => new N64RomInjector().InjectAsync(nes, null!));
    }

    private static BaseTitle Base() =>
        new(new TitleId(TitleType.Game, 0x10101D00), "Base", Region.UnitedStates, SourceConsole.N64);

    private static Game Game() =>
        new(new TitleId(TitleType.Demo, 0x1ABCDE00), GroupId.Parse("00001ABC"), ProductCode.Parse("WUP-N-TEST"));

    private static uint ReadUInt32(byte[] data, int at) =>
        (uint)data[at] << 24 | (uint)data[at + 1] << 16 | (uint)data[at + 2] << 8 | data[at + 3];

    private TitleDirectory StageBase()
    {
        var title = TitleDirectory.Create(Path.Combine(_root, "title", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(Path.Combine(title.Content, "rom"));
        Directory.CreateDirectory(Path.Combine(title.Content, "config"));
        File.WriteAllBytes(Path.Combine(title.Content, "rom", "NAAE.z64"), new byte[64]);
        File.WriteAllText(Path.Combine(title.Content, "config", "NAAE.z64.ini"), "old");
        File.WriteAllBytes(Path.Combine(title.Content, "FrameLayout.arc"), FakeFrameLayout.Build());
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
