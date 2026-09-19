using PD.WiiU.VirtualConsole.Options;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Gba.Tests;

[TestClass]
public class GbaTests
{
    private string _root = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Gba.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_root, recursive: true);

    [TestMethod]
    public void Goomba_Embedded_IsTheOfficialBuild()
    {
        var goomba = GoombaRom.Embedded();

        Assert.AreEqual(76020, goomba.Length);
        Assert.AreEqual("GOOMBA COLOR", System.Text.Encoding.ASCII.GetString(goomba, 0xA0, 12));
    }

    [TestMethod]
    public void Goomba_Wrap_PrependsEmulatorAndPadsTo32Mb()
    {
        var rom = Enumerable.Range(0, 1000).Select(i => (byte)i).ToArray();
        var goomba = new byte[] { 9, 8, 7 };

        var wrapped = GoombaRom.Wrap(rom, goomba);

        Assert.AreEqual(GoombaRom.PaddedSize, wrapped.Length);
        CollectionAssert.AreEqual(goomba, wrapped.Take(3).ToArray());
        CollectionAssert.AreEqual(rom, wrapped.Skip(3).Take(1000).ToArray());
        Assert.IsTrue(wrapped.Skip(1003).All(b => b == 0));
    }

    [TestMethod]
    public void Goomba_WrapTooLarge_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => GoombaRom.Wrap(new byte[GoombaRom.PaddedSize], new byte[1]));
        Assert.ThrowsExactly<ArgumentNullException>(() => GoombaRom.Wrap(null!));
    }

    [TestMethod]
    public void Goomba_IsGameBoy_ByExtension()
    {
        Assert.IsTrue(GoombaRom.IsGameBoy(@"C:\r\game.gb"));
        Assert.IsTrue(GoombaRom.IsGameBoy(@"C:\r\game.GBC"));
        Assert.IsFalse(GoombaRom.IsGameBoy(@"C:\r\game.gba"));
    }

    [TestMethod]
    public void PokemonPatch_TwoSites_ZeroesFourOrThreeBytesDependingOnTheFlag()
    {
        var rom = new byte[64];
        new byte[] { 0xD0, 0x88, 0x8D, 0x83, 0x42, 0x11, 0x22, 0x33, 0x44 }.CopyTo(rom, 4);
        new byte[] { 0xD0, 0x88, 0x8D, 0x83, 0x42, 0x11, 0x22, 0x33, 0x24 }.CopyTo(rom, 40);

        Assert.IsTrue(PokemonPatch.Apply(rom));

        CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 0 }, rom.Skip(9).Take(4).ToArray());
        CollectionAssert.AreEqual(new byte[] { 0, 0, 0, 0x24 }, rom.Skip(45).Take(4).ToArray());
    }

    [TestMethod]
    public void PokemonPatch_OneSite_LeavesRomAlone()
    {
        var rom = new byte[32];
        new byte[] { 0xD0, 0x88, 0x8D, 0x83, 0x42, 0x11, 0x22, 0x33, 0x44 }.CopyTo(rom, 4);
        var before = (byte[])rom.Clone();

        Assert.IsFalse(PokemonPatch.Apply(rom));
        CollectionAssert.AreEqual(before, rom);
    }

    [TestMethod]
    public async Task InjectAsync_GbaRom_ReplacesRomInArchiveAndKeepsOtherFiles()
    {
        var title = StageBase();
        var rom = Write("game.gba", Enumerable.Range(0, 5000).Select(i => (byte)(i * 7)).ToArray());
        var messages = new List<string>();

        await new GbaRomInjector().InjectAsync(Injection(rom), title, new SyncProgress(messages.Add));

        Assert.IsFalse(Directory.Exists(Path.Combine(title.Root, ".alldata")), "scratch removed");
        Assert.IsFalse(File.Exists(Path.Combine(title.Content, "alldata.psb")), "plain manifest removed");
        var (stored, brightness, files) = FakeAllData.Read(title.Content, Path.Combine(_root, "verify"));
        CollectionAssert.AreEqual(File.ReadAllBytes(rom), stored);
        Assert.AreEqual(0, brightness);
        CollectionAssert.AreEqual(new[] { "config/readme.txt", "config/title_prof.psb.m", "system/roms/AA88E0.D88.m" }, files, "the retail entry keeps its name and stays MDF-compressed");
        Assert.AreEqual(1, messages.Count);
    }

    [TestMethod]
    public async Task InjectAsync_PlainGbaEntry_ReplacesItInPlace()
    {
        var title = StageBase(plainRom: true);
        var rom = Write("game.gba", Enumerable.Range(0, 5000).Select(i => (byte)(i * 7)).ToArray());

        await new GbaRomInjector().InjectAsync(Injection(rom), title);

        var (stored, _, files) = FakeAllData.Read(title.Content, Path.Combine(_root, "verify"));
        CollectionAssert.AreEqual(File.ReadAllBytes(rom), stored);
        CollectionAssert.AreEqual(new[] { "config/readme.txt", "config/title_prof.psb.m", "rom/AGB-BASE.gba" }, files);
    }

    [TestMethod]
    public void WriteEntry_CompressedName_WritesAnMdfEntryAndNoPlainFile()
    {
        var folder = Path.Combine(_root, "entry");
        Directory.CreateDirectory(folder);
        var entry = Path.Combine(folder, "AA88E0.D88.m");
        var data = Enumerable.Range(0, 3000).Select(i => (byte)i).ToArray();

        AllDataArchive.WriteEntry(entry, data);

        Assert.IsTrue(File.Exists(entry));
        Assert.IsFalse(File.Exists(Path.Combine(folder, "AA88E0.D88")));
        CollectionAssert.AreEqual(new byte[] { 0x6D, 0x64, 0x66, 0x00 }, File.ReadAllBytes(entry).Take(4).ToArray(), "mdf magic");
        Assert.ThrowsExactly<ArgumentNullException>(() => AllDataArchive.WriteEntry(null!, data));
        Assert.ThrowsExactly<ArgumentNullException>(() => AllDataArchive.WriteEntry(entry, null!));
    }

    [TestMethod]
    public async Task InjectAsync_GameBoyRomWithDarkFilterRemoved_WrapsInGoombaAndSetsBrightness()
    {
        var title = StageBase();
        var gb = Write("game.gbc", Enumerable.Range(0, 300).Select(i => (byte)i).ToArray());
        var goomba = Write("goomba.gba", new byte[] { 1, 2, 3, 4 });
        var injection = new Injection(Base(), new Rom(gb, SourceConsole.Gba), Game()) { Options = new GbaOptions { RemoveDarkFilter = true } };

        await new GbaRomInjector(goomba).InjectAsync(injection, title);

        var (stored, brightness, _) = FakeAllData.Read(title.Content, Path.Combine(_root, "verify"));
        Assert.AreEqual(GoombaRom.PaddedSize, stored.Length);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, stored.Take(4).ToArray());
        CollectionAssert.AreEqual(File.ReadAllBytes(gb), stored.Skip(4).Take(300).ToArray());
        Assert.AreEqual(1, brightness);
    }

    [TestMethod]
    public void Constructor_GbaOrGameBoy_TakesTheConsoleAndRejectsOthers()
    {
        Assert.AreEqual(SourceConsole.Gba, new GbaRomInjector().Console);
        Assert.AreEqual(SourceConsole.Gba, new GbaRomInjector(SourceConsole.Gba).Console);
        Assert.AreEqual(SourceConsole.GameBoy, new GbaRomInjector(SourceConsole.GameBoy).Console);
        Assert.AreEqual(SourceConsole.GameBoy, new GbaRomInjector(null, SourceConsole.GameBoy).Console);
        Assert.AreEqual(TitleKind.VirtualConsole, new GbaRomInjector(SourceConsole.GameBoy).Kind);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new GbaRomInjector(SourceConsole.Nes));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new GbaRomInjector(null, SourceConsole.Snes));
    }

    [TestMethod]
    public async Task InjectAsync_GameBoyConsoleWithGbRom_StillWrapsInGoomba()
    {
        var title = StageBase();
        var gb = Write("game.gb", Enumerable.Range(0, 300).Select(i => (byte)i).ToArray());
        var goomba = Write("goomba.gba", new byte[] { 1, 2, 3, 4 });
        var @base = new BaseTitle(new TitleId(TitleType.Game, 0x10101D00), "Base", Region.UnitedStates, SourceConsole.GameBoy);
        var injection = new Injection(@base, new Rom(gb, SourceConsole.GameBoy), Game()) { Options = new GbaOptions { Console = SourceConsole.GameBoy } };
        var messages = new List<string>();

        await new GbaRomInjector(goomba, SourceConsole.GameBoy).InjectAsync(injection, title, new SyncProgress(messages.Add));

        var (stored, brightness, _) = FakeAllData.Read(title.Content, Path.Combine(_root, "verify"));
        Assert.AreEqual(GoombaRom.PaddedSize, stored.Length);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, stored.Take(4).ToArray());
        CollectionAssert.AreEqual(File.ReadAllBytes(gb), stored.Skip(4).Take(300).ToArray());
        Assert.AreEqual(0, brightness, "no dark filter change was asked for");
        CollectionAssert.Contains(messages, "Wrapping Game Boy ROM in Goomba");
    }

    [TestMethod]
    public async Task InjectAsync_GameBoyConsoleGivenAGbaInjection_ThrowsArgumentException()
    {
        var title = StageBase();

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => new GbaRomInjector(SourceConsole.GameBoy).InjectAsync(Injection("x.gba"), title));
    }

    [TestMethod]
    public async Task InjectAsync_NoArchive_ThrowsFileNotFoundException()
    {
        var title = TitleDirectory.Create(Path.Combine(_root, "empty"));
        var rom = Write("game.gba", new byte[16]);

        await Assert.ThrowsExactlyAsync<FileNotFoundException>(() => new GbaRomInjector().InjectAsync(Injection(rom), title));
    }

    [TestMethod]
    public async Task InjectAsync_WrongConsoleOrNulls_Throw()
    {
        var title = StageBase();
        var nes = new Injection(new BaseTitle(new TitleId(TitleType.Game, 1), "NES", Region.UnitedStates, SourceConsole.Nes), new Rom("x.nes", SourceConsole.Nes), Game());

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => new GbaRomInjector().InjectAsync(nes, title));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => new GbaRomInjector().InjectAsync(null!, title));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => new GbaRomInjector().InjectAsync(Injection("x.gba"), null!));
    }

    [TestMethod]
    public void Inspect_ArchivePresent_PassesAndMissingPartsAreListed()
    {
        var good = StageBase();
        var empty = TitleDirectory.Create(Path.Combine(_root, "empty"));

        Assert.AreEqual(0, new GbaRomInjector().Inspect(good).Count);
        CollectionAssert.AreEqual(new[] { "content/alldata.psb.m: file missing", "content/alldata.bin: file missing" }, new GbaRomInjector().Inspect(empty).Select(i => i.ToString()).ToArray());
        Assert.ThrowsExactly<ArgumentNullException>(() => new GbaRomInjector().Inspect(null!));
    }

    private static BaseTitle Base() =>
        new(new TitleId(TitleType.Game, 0x10101D00), "Base", Region.UnitedStates, SourceConsole.Gba);

    private static Game Game() =>
        new(new TitleId(TitleType.Demo, 0x1ABCDE00), GroupId.Parse("00001ABC"), ProductCode.Parse("WUP-N-TEST"));

    private static Injection Injection(string rom) =>
        new(Base(), new Rom(rom, SourceConsole.Gba), Game());

    private TitleDirectory StageBase(bool plainRom = false)
    {
        var title = TitleDirectory.Create(Path.Combine(_root, "title", Guid.NewGuid().ToString("N")));
        FakeAllData.Build(title.Content, Path.Combine(_root, "build"), plainRom: plainRom);
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
