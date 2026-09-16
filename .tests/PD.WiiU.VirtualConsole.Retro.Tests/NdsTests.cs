using System.IO.Compression;
using System.Text.Json.Nodes;
using PD.WiiU.VirtualConsole.Options;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Retro.Tests;

[TestClass]
public class NdsTests
{
    private const string Configuration = "{\"configuration\":{\"Display\":{\"Brightness\":80,\"PixelArtUpscaler\":0,\"RenderScale\":2},\"Other\":{\"Keep\":true}}}";

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
    public async Task InjectAsync_Rom_ReplacesZipEntryUnderTheBaseName()
    {
        var title = StageBase();
        var rom = Write("game.nds", Enumerable.Range(0, 4000).Select(i => (byte)(i * 5)).ToArray());
        var messages = new List<string>();

        await new NdsRomInjector().InjectAsync(Injection(rom), title, new SyncProgress(messages.Add));

        using var zip = ZipFile.OpenRead(Path.Combine(title.Content, "0010", "rom.zip"));
        var entry = zip.Entries.Single();
        Assert.AreEqual("WUP-XYZ.nds", entry.Name);
        using var stream = entry.Open();
        var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        CollectionAssert.AreEqual(File.ReadAllBytes(rom), buffer.ToArray());
        Assert.AreEqual(Configuration, File.ReadAllText(Path.Combine(title.Content, "0010", "configuration_cafe.json")), "settings untouched at defaults");
        Assert.AreEqual(1, messages.Count);
    }

    [TestMethod]
    public async Task InjectAsync_DisplayOptions_UpdatesConfigurationAndKeepsOtherFields()
    {
        var title = StageBase();
        var rom = Write("game.nds", new byte[100]);
        var injection = new Injection(Base(), new Rom(rom, SourceConsole.Nds), Game()) { Options = new NdsOptions { Brightness = 100, PixelArtUpscaler = 16 } };

        await new NdsRomInjector().InjectAsync(injection, title);

        var root = JsonNode.Parse(File.ReadAllText(Path.Combine(title.Content, "0010", "configuration_cafe.json")))!;
        Assert.AreEqual(100, (int)root["configuration"]!["Display"]!["Brightness"]!);
        Assert.AreEqual(16, (int)root["configuration"]!["Display"]!["PixelArtUpscaler"]!);
        Assert.AreEqual(2, (int)root["configuration"]!["Display"]!["RenderScale"]!);
        Assert.IsTrue((bool)root["configuration"]!["Other"]!["Keep"]!);
    }

    [TestMethod]
    public async Task InjectAsync_LayoutScreens_CopiesFolderOverTheTitle()
    {
        var title = StageBase();
        var rom = Write("game.nds", new byte[100]);
        var screens = Path.Combine(_root, "screens");
        Directory.CreateDirectory(Path.Combine(screens, "content", "layout"));
        File.WriteAllText(Path.Combine(screens, "content", "layout", "top.png"), "png");
        Directory.CreateDirectory(Path.Combine(screens, "meta"));
        File.WriteAllText(Path.Combine(screens, "meta", "iconTex.tga"), "x");
        var injection = new Injection(Base(), new Rom(rom, SourceConsole.Nds), Game()) { Options = new NdsOptions { LayoutScreensPath = screens } };

        await new NdsRomInjector().InjectAsync(injection, title);

        Assert.AreEqual("png", File.ReadAllText(Path.Combine(title.Content, "layout", "top.png")));
        Assert.AreEqual("x", File.ReadAllText(Path.Combine(title.Meta, "iconTex.tga")));
    }

    [TestMethod]
    public async Task InjectAsync_LayoutPack_LaysTheBundledFilesOverTheTitle()
    {
        var title = StageBase();
        var rom = Write("game.nds", new byte[100]);
        var injection = new Injection(Base(), new Rom(rom, SourceConsole.Nds), Game()) { Options = new NdsOptions { LayoutPack = NdsLayoutPack.PhantomHourglass, Brightness = 50 } };
        var messages = new List<string>();

        await new NdsRomInjector().InjectAsync(injection, title, new SyncProgress(messages.Add));

        Assert.IsTrue(File.Exists(Path.Combine(title.Content, "0010", "assets", "textures", "sidewayslitetv.png")));
        Assert.IsTrue(File.Exists(Path.Combine(title.Content, "0010", "data", "strings", "en", "strings.json")));
        var configuration = File.ReadAllText(Path.Combine(title.Content, "0010", "configuration_cafe.json"));
        StringAssert.Contains(configuration, "50", "the pack's configuration still takes the brightness afterwards");
        CollectionAssert.Contains(messages, "Adding the PhantomHourglass layout screens");
    }

    [TestMethod]
    public async Task InjectAsync_LayoutFolderAndPack_FolderWins()
    {
        var title = StageBase();
        var rom = Write("game.nds", new byte[100]);
        var screens = Path.Combine(_root, "screens");
        Directory.CreateDirectory(Path.Combine(screens, "content"));
        File.WriteAllText(Path.Combine(screens, "content", "mine.png"), "png");
        var injection = new Injection(Base(), new Rom(rom, SourceConsole.Nds), Game()) { Options = new NdsOptions { LayoutPack = NdsLayoutPack.All, LayoutScreensPath = screens } };

        await new NdsRomInjector().InjectAsync(injection, title);

        Assert.IsTrue(File.Exists(Path.Combine(title.Content, "mine.png")));
        Assert.IsFalse(File.Exists(Path.Combine(title.Content, "0010", "assets", "textures", "ndslite.png")));
    }

    [TestMethod]
    public void DsLayoutScreens_Extract_WritesEachPackAndNothingForNone()
    {
        var all = Path.Combine(_root, "all");
        var hourglass = Path.Combine(_root, "hourglass");

        Assert.AreEqual(12, DsLayoutScreens.Extract(NdsLayoutPack.All, all));
        Assert.AreEqual(12, DsLayoutScreens.Extract(NdsLayoutPack.PhantomHourglass, hourglass));
        Assert.AreEqual(0, DsLayoutScreens.Extract(NdsLayoutPack.None, Path.Combine(_root, "none")));

        Assert.IsFalse(Directory.Exists(Path.Combine(_root, "none")));
        Assert.IsTrue(File.Exists(Path.Combine(all, "content", "0010", "data", "images", "vcmenus_ntr.png")));
        Assert.AreNotEqual(new FileInfo(Path.Combine(all, "content", "0010", "configuration_cafe.json")).Length, 0);
        Assert.IsFalse(File.ReadAllBytes(Path.Combine(all, "content", "0010", "configuration_cafe.json")).SequenceEqual(File.ReadAllBytes(Path.Combine(hourglass, "content", "0010", "configuration_cafe.json"))), "the packs differ in their configuration");
        Assert.ThrowsExactly<ArgumentNullException>(() => DsLayoutScreens.Extract(NdsLayoutPack.All, null!));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => DsLayoutScreens.Extract((NdsLayoutPack)9, _root));
    }

    [TestMethod]
    public async Task InjectAsync_NoWupEntry_ThrowsInvalidDataException()
    {
        var title = StageBase();
        var archive = Path.Combine(title.Content, "0010", "rom.zip");
        File.Delete(archive);
        using (var zip = ZipFile.Open(archive, ZipArchiveMode.Create))
            zip.CreateEntry("other.nds");
        var rom = Write("game.nds", new byte[10]);

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => new NdsRomInjector().InjectAsync(Injection(rom), title));
    }

    [TestMethod]
    public async Task InjectAsync_NoArchive_ThrowsFileNotFoundException()
    {
        var title = TitleDirectory.Create(Path.Combine(_root, "empty"));
        var rom = Write("game.nds", new byte[10]);

        await Assert.ThrowsExactlyAsync<FileNotFoundException>(() => new NdsRomInjector().InjectAsync(Injection(rom), title));
    }

    [TestMethod]
    public async Task InjectAsync_WrongConsoleOrNulls_Throw()
    {
        var title = StageBase();
        var nes = new Injection(new BaseTitle(new TitleId(TitleType.Game, 1), "NES", Region.UnitedStates, SourceConsole.Nes), new Rom("x.nes", SourceConsole.Nes), Game());

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => new NdsRomInjector().InjectAsync(nes, title));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => new NdsRomInjector().InjectAsync(null!, title));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => new NdsRomInjector().InjectAsync(Injection("x.nds"), null!));
    }

    private static BaseTitle Base() =>
        new(new TitleId(TitleType.Game, 0x10101D00), "Base", Region.UnitedStates, SourceConsole.Nds);

    private static Game Game() =>
        new(new TitleId(TitleType.Demo, 0x1ABCDE00), GroupId.Parse("00001ABC"), ProductCode.Parse("WUP-N-TEST"));

    private static Injection Injection(string rom) =>
        new(Base(), new Rom(rom, SourceConsole.Nds), Game());

    private TitleDirectory StageBase()
    {
        var title = TitleDirectory.Create(Path.Combine(_root, "title", Guid.NewGuid().ToString("N")));
        var data = Path.Combine(title.Content, "0010");
        Directory.CreateDirectory(data);
        using (var zip = ZipFile.Open(Path.Combine(data, "rom.zip"), ZipArchiveMode.Create))
        {
            using var entry = zip.CreateEntry("WUP-XYZ.nds").Open();
            entry.Write(new byte[64], 0, 64);
        }
        File.WriteAllText(Path.Combine(data, "configuration_cafe.json"), Configuration);
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
