using System.Text;
using PD.WiiU.VirtualConsole.Options;

namespace PD.WiiU.VirtualConsole.RetroArch.Tests;

[TestClass]
public class RetroArchRomInjectorTests
{
    private const string EmptyCos = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<app type=\"complex\" access=\"777\">\n  <argstr type=\"string\" length=\"4096\"></argstr>\n</app>";
    private const string RpxName = "genesis_plus_gx_libretro.rpx";

    private string _root = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = TestPaths.TempRoot();
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void Console_Constructed_IsWhatTheConstructorGot()
    {
        Assert.AreEqual(SourceConsole.Genesis, new RetroArchRomInjector(SourceConsole.Genesis).Console);
        Assert.AreEqual(SourceConsole.Nes, new RetroArchRomInjector(SourceConsole.Nes).Console);
    }

    [TestMethod]
    public void ContentFileName_BlankInput_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => RetroArchRomInjector.ContentFileName(" "));
        Assert.ThrowsExactly<ArgumentException>(() => RetroArchRomInjector.ContentFileName(null!));
    }

    [TestMethod]
    public void ContentFileName_NameWithSpacesAndPunctuation_ReplacesThemWithUnderscores()
    {
        var name = RetroArchRomInjector.ContentFileName(@"C:\roms\Sonic The Hedgehog (USA, Europe).MD");

        Assert.AreEqual("Sonic_The_Hedgehog_USA_Europe.md", name);
        Assert.IsFalse(name.Any(c => c == ' ' || c == '(' || c == ')' || c == ','));
    }

    [TestMethod]
    public void ContentFileName_OnlySymbols_FallsBackToGame()
    {
        Assert.AreEqual("game.md", RetroArchRomInjector.ContentFileName("(((.MD"));
    }

    [TestMethod]
    public void ContentFileName_SafeName_IsUnchanged()
    {
        Assert.AreEqual("Sonic.md", RetroArchRomInjector.ContentFileName("Sonic.md"));
    }

    [TestMethod]
    public async Task InjectAsync_ArcadeCompanions_CopiesThemBesideTheRomUnderTheirOwnNames()
    {
        var title = StageFake();
        var rom = WriteRom("mslug.zip", new byte[] { 1, 2, 3 });
        var bios = WriteRom("neogeo.zip", new byte[] { 4, 5 });
        var parent = WriteRom("parent set (rev A).zip", new byte[] { 6 });
        var messages = new List<string>();
        var injection = ArcadeInjection(rom, SourceConsole.NeoGeo, bios, parent);

        await new RetroArchRomInjector(SourceConsole.NeoGeo).InjectAsync(injection, title, new SyncProgress(messages.Add));

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, File.ReadAllBytes(Path.Combine(title.Content, "mslug.zip")));
        CollectionAssert.AreEqual(new byte[] { 4, 5 }, File.ReadAllBytes(Path.Combine(title.Content, "neogeo.zip")));
        CollectionAssert.AreEqual(new byte[] { 6 }, File.ReadAllBytes(Path.Combine(title.Content, "parent set (rev A).zip")), "name kept verbatim, space and all");
        Assert.AreEqual(RpxName + " fs:/vol/content/mslug.zip", CosXml.Load(Path.Combine(title.Code, CosXml.FileName)).Arguments);
        CollectionAssert.AreEqual(
            new[] { "Copying mslug.zip as mslug.zip", "Copying neogeo.zip beside it", "Copying parent set (rev A).zip beside it", "Pointing cos.xml at it" },
            messages);
    }

    [TestMethod]
    public async Task InjectAsync_CompanionNamedLikeTheRom_IsSkipped()
    {
        var title = StageFake();
        var rom = WriteRom("mslug.zip", new byte[] { 1, 2, 3 });
        var other = Path.Combine(_root, "other");
        Directory.CreateDirectory(other);
        var duplicate = Path.Combine(other, "mslug.zip");
        File.WriteAllBytes(duplicate, new byte[] { 9, 9, 9 });
        var messages = new List<string>();

        await new RetroArchRomInjector(SourceConsole.Arcade).InjectAsync(ArcadeInjection(rom, SourceConsole.Arcade, duplicate), title, new SyncProgress(messages.Add));

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, File.ReadAllBytes(Path.Combine(title.Content, "mslug.zip")));
        Assert.AreEqual(1, Directory.GetFiles(title.Content).Length);
        Assert.AreEqual(2, messages.Count);
    }

    [TestMethod]
    public async Task InjectAsync_MissingCompanion_ThrowsFileNotFoundException()
    {
        var title = StageFake();
        var rom = WriteRom("mslug.zip", new byte[] { 1 });
        var injection = ArcadeInjection(rom, SourceConsole.Arcade, Path.Combine(_root, "nope.zip"));

        await Assert.ThrowsExactlyAsync<FileNotFoundException>(() => new RetroArchRomInjector(SourceConsole.Arcade).InjectAsync(injection, title));
    }

    [TestMethod]
    public async Task InjectAsync_ArcadeWithoutOptions_CopiesOnlyTheRom()
    {
        var title = StageFake();
        var rom = WriteRom("mslug.zip", new byte[] { 1 });
        var injection = new Injection(new RetroArchCore("fbneo", "FinalBurn Neo", SourceConsole.Arcade, "d"), new Rom(rom, SourceConsole.Arcade), Game());
        var messages = new List<string>();

        await new RetroArchRomInjector(SourceConsole.Arcade).InjectAsync(injection, title, new SyncProgress(messages.Add));

        Assert.AreEqual(1, Directory.GetFiles(title.Content).Length);
        Assert.AreEqual(2, messages.Count);
    }

    [TestMethod]
    public async Task InjectAsync_HappyPath_CopiesRomAndPointsCosAtIt()
    {
        var title = StageFake();
        var bytes = Enumerable.Range(0, 4096).Select(i => (byte)(i * 7)).ToArray();
        var rom = WriteRom("My Game (U) [!].bin", bytes);
        var messages = new List<string>();

        await new RetroArchRomInjector(SourceConsole.Genesis).InjectAsync(Injection(rom), title, new SyncProgress(messages.Add));

        var copied = Path.Combine(title.Content, "My_Game_U.bin");
        Assert.IsTrue(File.Exists(copied), "ROM copied under content");
        CollectionAssert.AreEqual(bytes, File.ReadAllBytes(copied));
        Assert.AreEqual(RpxName + " fs:/vol/content/My_Game_U.bin", CosXml.Load(Path.Combine(title.Code, CosXml.FileName)).Arguments);
        Assert.AreEqual(2, messages.Count);
    }

    [TestMethod]
    public async Task InjectAsync_InjectionForAnotherConsole_ThrowsArgumentException()
    {
        var title = StageFake();
        var rom = WriteRom("game.nes", new byte[] { 1 });
        var injection = new Injection(new RetroArchCore("nestopia", "Nestopia", SourceConsole.Nes, "d"), new Rom(rom, SourceConsole.Nes), Game());

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => new RetroArchRomInjector(SourceConsole.Genesis).InjectAsync(injection, title));
    }

    [TestMethod]
    public async Task InjectAsync_NullArguments_ThrowsArgumentNullException()
    {
        var injector = new RetroArchRomInjector(SourceConsole.Genesis);
        var title = StageFake();
        var injection = Injection(WriteRom("game.bin", new byte[] { 1 }));

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => injector.InjectAsync(null!, title));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => injector.InjectAsync(injection, null!));
    }

    [TestMethod]
    public async Task InjectAsync_TitleWithoutRpx_ThrowsFileNotFoundException()
    {
        var title = StageFake();
        File.Delete(Path.Combine(title.Code, RpxName));
        var injection = Injection(WriteRom("game.bin", new byte[] { 1 }));

        await Assert.ThrowsExactlyAsync<FileNotFoundException>(() => new RetroArchRomInjector(SourceConsole.Genesis).InjectAsync(injection, title));
    }

    [TestMethod]
    public void Inspect_EmptyTitle_ReportsMissingRpxAndCos()
    {
        var title = TitleDirectory.Create(Path.Combine(_root, "title"));

        var issues = new RetroArchRomInjector(SourceConsole.Genesis).Inspect(title);

        Assert.AreEqual(2, issues.Count);
        Assert.AreEqual("code", issues[0].Path);
        StringAssert.Contains(issues[0].Message, RetroArchCore.RpxSuffix);
        Assert.AreEqual("code/cos.xml", issues[1].Path);
        Assert.AreEqual("file missing", issues[1].Message);
    }

    [TestMethod]
    public void Inspect_Null_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new RetroArchRomInjector(SourceConsole.Genesis).Inspect(null!));
    }

    [TestMethod]
    public void Inspect_StagedFakeTitle_IsClean()
    {
        Assert.AreEqual(0, new RetroArchRomInjector(SourceConsole.Genesis).Inspect(StageFake()).Count);
    }

    private static Injection ArcadeInjection(string romPath, SourceConsole console, params string[] companions) =>
        new(new RetroArchCore("fbneo", "FinalBurn Neo", console, "d"), new Rom(romPath, console), Game())
        {
            Options = new ArcadeOptions { Console = console, CompanionPaths = companions },
        };

    private static WiiUSharp.Game Game() => GameFactory.Create("Name", null, null, false, new Random(1));

    private static Injection Injection(string romPath) =>
        new(new RetroArchCore("genesis_plus_gx", "Genesis Plus GX", SourceConsole.Genesis, "d"), new Rom(romPath, SourceConsole.Genesis), Game());

    private TitleDirectory StageFake()
    {
        var title = TitleDirectory.Create(Path.Combine(_root, "title"));
        File.WriteAllBytes(Path.Combine(title.Code, RpxName), new byte[] { 0x7F, (byte)'E', (byte)'L', (byte)'F' });
        File.WriteAllText(Path.Combine(title.Code, CosXml.FileName), EmptyCos, new UTF8Encoding(false));
        return title;
    }

    private string WriteRom(string name, byte[] bytes)
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
