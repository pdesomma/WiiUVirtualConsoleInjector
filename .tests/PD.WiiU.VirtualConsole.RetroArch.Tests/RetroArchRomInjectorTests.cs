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
    public async Task InjectAsync_Chd_CopiesNothingExtra()
    {
        var title = StageFake();
        var rom = WriteRom("Game (USA).chd", new byte[] { 1, 2 });
        var messages = new List<string>();

        await new RetroArchRomInjector(SourceConsole.PlayStation).InjectAsync(PlayStationInjection(rom), title, new SyncProgress(messages.Add));

        CollectionAssert.AreEqual(new[] { "Game_USA.chd" }, Directory.GetFiles(title.Content).Select(p => Path.GetFileName(p)).ToArray());
        Assert.AreEqual(RpxName + " fs:/vol/content/Game_USA.chd", CosXml.Load(Path.Combine(title.Code, CosXml.FileName)).Arguments);
        Assert.AreEqual(2, messages.Count);
    }

    [TestMethod]
    public async Task InjectAsync_CueAndBin_CopiesTheBinVerbatimBesideTheSanitisedCue()
    {
        var title = StageFake();
        var cueText = "FILE \"Game (USA) (Track 1).bin\" BINARY\r\n  TRACK 01 MODE2/2352\r\n    INDEX 01 00:00:00\r\n";
        var rom = WriteText("Game (USA).cue", cueText);
        WriteRom("Game (USA) (Track 1).bin", new byte[] { 7, 8, 9 });
        var messages = new List<string>();

        await new RetroArchRomInjector(SourceConsole.PlayStation).InjectAsync(PlayStationInjection(rom), title, new SyncProgress(messages.Add));

        Assert.AreEqual(cueText, File.ReadAllText(Path.Combine(title.Content, "Game_USA.cue")), "flat already, copied as is");
        CollectionAssert.AreEqual(new byte[] { 7, 8, 9 }, File.ReadAllBytes(Path.Combine(title.Content, "Game (USA) (Track 1).bin")));
        Assert.AreEqual(2, Directory.GetFiles(title.Content).Length);
        Assert.AreEqual(RpxName + " fs:/vol/content/Game_USA.cue", CosXml.Load(Path.Combine(title.Code, CosXml.FileName)).Arguments);
        CollectionAssert.AreEqual(
            new[] { "Copying Game (USA).cue as Game_USA.cue", "Copying Game (USA) (Track 1).bin beside it", "Pointing cos.xml at it" },
            messages);
    }

    [TestMethod]
    public async Task InjectAsync_CueWithSubfolderTrack_FlattensTheCopiedCueAndLandsTheBinFlat()
    {
        var title = StageFake();
        Directory.CreateDirectory(Path.Combine(_root, "subdir"));
        var rom = WriteText("game.cue", "FILE \"subdir/track.bin\" BINARY\r\n  TRACK 01 MODE2/2352\r\n");
        WriteRom(Path.Combine("subdir", "track.bin"), new byte[] { 1 });

        await new RetroArchRomInjector(SourceConsole.PlayStation).InjectAsync(PlayStationInjection(rom), title);

        Assert.AreEqual("FILE \"track.bin\" BINARY\r\n  TRACK 01 MODE2/2352\r\n", File.ReadAllText(Path.Combine(title.Content, "game.cue")));
        Assert.IsTrue(File.Exists(Path.Combine(title.Content, "track.bin")));
        Assert.AreEqual(2, Directory.GetFiles(title.Content).Length);
        Assert.AreEqual(0, Directory.GetDirectories(title.Content).Length);
    }

    [TestMethod]
    public async Task InjectAsync_CueWithMissingBin_ThrowsFileNotFoundExceptionBeforeTouchingAnything()
    {
        var title = StageFake();
        var rom = WriteText("game.cue", "FILE \"nope.bin\" BINARY\r\n");

        await Assert.ThrowsExactlyAsync<FileNotFoundException>(() => new RetroArchRomInjector(SourceConsole.PlayStation).InjectAsync(PlayStationInjection(rom), title));

        Assert.AreEqual(0, Directory.GetFiles(title.Content).Length);
        Assert.AreEqual(EmptyCos, File.ReadAllText(Path.Combine(title.Code, CosXml.FileName)));
    }

    [TestMethod]
    public async Task InjectAsync_M3uOfTwoDiscs_LandsCuesAndBins()
    {
        var title = StageFake();
        WriteRom("Game (Disc 1).bin", new byte[] { 1 });
        WriteRom("Game (Disc 2).bin", new byte[] { 2 });
        WriteText("Game (Disc 1).cue", "FILE \"Game (Disc 1).bin\" BINARY\n  TRACK 01 MODE2/2352\n");
        WriteText("Game (Disc 2).cue", "FILE \"Game (Disc 2).bin\" BINARY\n  TRACK 01 MODE2/2352\n");
        var rom = WriteText("Game (USA).m3u", "Game (Disc 1).cue\nGame (Disc 2).cue\n");
        var messages = new List<string>();

        await new RetroArchRomInjector(SourceConsole.PlayStation).InjectAsync(PlayStationInjection(rom), title, new SyncProgress(messages.Add));

        CollectionAssert.AreEquivalent(
            new[] { "Game_USA.m3u", "Game (Disc 1).cue", "Game (Disc 1).bin", "Game (Disc 2).cue", "Game (Disc 2).bin" },
            Directory.GetFiles(title.Content).Select(p => Path.GetFileName(p)).ToArray());
        Assert.AreEqual("Game (Disc 1).cue\nGame (Disc 2).cue\n", File.ReadAllText(Path.Combine(title.Content, "Game_USA.m3u")));
        Assert.AreEqual(RpxName + " fs:/vol/content/Game_USA.m3u", CosXml.Load(Path.Combine(title.Code, CosXml.FileName)).Arguments);
        CollectionAssert.AreEqual(
            new[] { "Copying Game (USA).m3u as Game_USA.m3u", "Copying Game (Disc 1).cue beside it", "Copying Game (Disc 1).bin beside it", "Copying Game (Disc 2).cue beside it", "Copying Game (Disc 2).bin beside it", "Pointing cos.xml at it" },
            messages);
    }

    [TestMethod]
    public async Task InjectAsync_Commodore64M3uOfTwoDisks_LandsBothD64sBesideIt()
    {
        var title = StageFake();
        WriteRom("Game (Disk 1).d64", new byte[] { 1 });
        WriteRom("Game (Disk 2).d64", new byte[] { 2 });
        var rom = WriteText("Game (Europe).m3u", "Game (Disk 1).d64\nGame (Disk 2).d64\n");
        var injection = new Injection(new RetroArchCore("vice_x64", "VICE x64", SourceConsole.Commodore64, "d"), new Rom(rom, SourceConsole.Commodore64), Game());
        var messages = new List<string>();

        await new RetroArchRomInjector(SourceConsole.Commodore64).InjectAsync(injection, title, new SyncProgress(messages.Add));

        CollectionAssert.AreEquivalent(
            new[] { "Game_Europe.m3u", "Game (Disk 1).d64", "Game (Disk 2).d64" },
            Directory.GetFiles(title.Content).Select(p => Path.GetFileName(p)).ToArray());
        CollectionAssert.AreEqual(new byte[] { 1 }, File.ReadAllBytes(Path.Combine(title.Content, "Game (Disk 1).d64")));
        CollectionAssert.AreEqual(new byte[] { 2 }, File.ReadAllBytes(Path.Combine(title.Content, "Game (Disk 2).d64")));
        Assert.AreEqual("Game (Disk 1).d64\nGame (Disk 2).d64\n", File.ReadAllText(Path.Combine(title.Content, "Game_Europe.m3u")));
        Assert.AreEqual(RpxName + " fs:/vol/content/Game_Europe.m3u", CosXml.Load(Path.Combine(title.Code, CosXml.FileName)).Arguments);
        CollectionAssert.AreEqual(
            new[] { "Copying Game (Europe).m3u as Game_Europe.m3u", "Copying Game (Disk 1).d64 beside it", "Copying Game (Disk 2).d64 beside it", "Pointing cos.xml at it" },
            messages);
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

    private static Injection PlayStationInjection(string romPath) =>
        new(new RetroArchCore("pcsx_rearmed", "PCSX-ReARMed", SourceConsole.PlayStation, "d"), new Rom(romPath, SourceConsole.PlayStation), Game());

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

    private string WriteText(string name, string text)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllText(path, text, new UTF8Encoding(false));
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
