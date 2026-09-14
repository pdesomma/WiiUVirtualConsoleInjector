using System.Text;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Retro.Tests;

[TestClass]
public class Tg16Tests
{
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
    public void BuildHuCard_Rom_WritesConfigThenRomTwice()
    {
        var rom = Enumerable.Range(0, 300).Select(i => (byte)i).ToArray();

        var package = PcePackage.BuildHuCard("Game.pce", rom);

        var entries = Read(package);
        Assert.AreEqual(3, entries.Count);
        Assert.AreEqual("pceconfig.bin", entries[0].Name);
        Assert.AreEqual(0xA0, entries[0].Data.Length);
        CollectionAssert.AreEqual(new byte[32], entries[0].Data.Take(32).ToArray());
        CollectionAssert.AreEqual(Padded("Game.pce"), entries[0].Data.Skip(32).Take(64).ToArray());
        CollectionAssert.AreEqual(Padded("Game.pce"), entries[0].Data.Skip(96).Take(64).ToArray());
        Assert.AreEqual("Game.pce", entries[1].Name);
        CollectionAssert.AreEqual(rom, entries[1].Data);
        Assert.AreEqual("Game.pce", entries[2].Name);
        CollectionAssert.AreEqual(rom, entries[2].Data);
    }

    [TestMethod]
    public void BuildHuCard_TotalLength_IsFileSizeMinusFour()
    {
        var package = PcePackage.BuildHuCard("a.pce", new byte[10]);

        Assert.AreEqual((uint)(package.Length - 4), ReadLength(package, 0));
    }

    [TestMethod]
    public void BuildHuCard_LongName_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => PcePackage.BuildHuCard(new string('x', 65), new byte[1]));
        Assert.ThrowsExactly<ArgumentException>(() => PcePackage.BuildHuCard("", new byte[1]));
        Assert.ThrowsExactly<ArgumentNullException>(() => PcePackage.BuildHuCard("a.pce", null!));
    }

    [TestMethod]
    public void BuildTurboCd_Folder_WritesConfigHcdAndListedFilesUnderTestFolder()
    {
        var folder = TurboCdFolder();

        var package = PcePackage.BuildTurboCd(folder);

        var entries = Read(package);
        CollectionAssert.AreEqual(new[] { "pceconfig.bin", "test/game.hcd", "test/track01.ogg", "test/data.bin" }, entries.Select(e => e.Name).ToArray());
        CollectionAssert.AreEqual(new byte[] { 1, 0, 0, 128 }, entries[0].Data.Take(4).ToArray());
        Assert.AreEqual(1, entries[0].Data[16]);
        CollectionAssert.AreEqual(Padded("test/game.hcd"), entries[0].Data.Skip(32).Take(64).ToArray());
        Assert.AreEqual("ogg1", Encoding.ASCII.GetString(entries[2].Data));
        Assert.AreEqual("bin1", Encoding.ASCII.GetString(entries[3].Data));
    }

    [TestMethod]
    public void BuildTurboCd_MissingKinds_ThrowsFileNotFoundException()
    {
        var folder = TurboCdFolder();
        File.Delete(Path.Combine(folder, "track01.ogg"));

        Assert.ThrowsExactly<FileNotFoundException>(() => PcePackage.BuildTurboCd(folder));
    }

    [TestMethod]
    public void BuildTurboCd_TwoHcds_ThrowsInvalidDataException()
    {
        var folder = TurboCdFolder();
        File.WriteAllText(Path.Combine(folder, "other.hcd"), "");

        Assert.ThrowsExactly<InvalidDataException>(() => PcePackage.BuildTurboCd(folder));
    }

    [TestMethod]
    public void ListedFiles_Hcd_TakesThirdColumnAndSkipsShortLines()
    {
        var hcd = Path.Combine(_root, "x.hcd");
        File.WriteAllText(hcd, "1,AUDIO,track01.ogg\r\n2,DATA,data.bin\r\nbroken\r\n\r\n3,AUDIO, spaced.ogg \r\n");

        CollectionAssert.AreEqual(new[] { "track01.ogg", "data.bin", "spaced.ogg" }, PcePackage.ListedFiles(hcd).ToArray());
    }

    [TestMethod]
    public async Task InjectAsync_HuCard_WritesPackageIntoPceemu()
    {
        var title = TitleDirectory.Create(Path.Combine(_root, "title"));
        Directory.CreateDirectory(Path.Combine(title.Content, "pceemu"));
        File.WriteAllBytes(Path.Combine(title.Content, "pceemu", "pce.pkg"), new byte[] { 9, 9 });
        var rom = Path.Combine(_root, "Bonk.pce");
        File.WriteAllBytes(rom, new byte[] { 1, 2, 3 });

        await new Tg16RomInjector().InjectAsync(Injection(rom), title);

        var entries = Read(File.ReadAllBytes(Path.Combine(title.Content, "pceemu", "pce.pkg")));
        Assert.AreEqual("Bonk.pce", entries[1].Name);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, entries[2].Data);
    }

    [TestMethod]
    public async Task InjectAsync_Folder_PacksTurboCd()
    {
        var title = TitleDirectory.Create(Path.Combine(_root, "title"));
        var folder = TurboCdFolder();

        await new Tg16RomInjector().InjectAsync(Injection(folder), title);

        var entries = Read(File.ReadAllBytes(Path.Combine(title.Content, "pceemu", "pce.pkg")));
        Assert.AreEqual("test/game.hcd", entries[1].Name);
    }

    [TestMethod]
    public async Task InjectAsync_WrongConsoleOrNulls_Throw()
    {
        var title = TitleDirectory.Create(Path.Combine(_root, "title"));
        var nes = new Injection(new BaseTitle(new TitleId(TitleType.Game, 1), "NES", Region.UnitedStates, SourceConsole.Nes), new Rom("x.nes", SourceConsole.Nes), Game());

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => new Tg16RomInjector().InjectAsync(nes, title));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => new Tg16RomInjector().InjectAsync(null!, title));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => new Tg16RomInjector().InjectAsync(Injection("x.pce"), null!));
    }

    private static Game Game() =>
        new(new TitleId(TitleType.Demo, 0x1ABCDE00), GroupId.Parse("00001ABC"), ProductCode.Parse("WUP-N-TEST"));

    private static Injection Injection(string rom) =>
        new(new BaseTitle(new TitleId(TitleType.Game, 0x10101D00), "Base", Region.UnitedStates, SourceConsole.Tg16), new Rom(rom, SourceConsole.Tg16), Game());

    private static byte[] Padded(string name)
    {
        var bytes = new byte[64];
        Encoding.ASCII.GetBytes(name).CopyTo(bytes, 0);
        return bytes;
    }

    private static List<(string Name, byte[] Data)> Read(byte[] package)
    {
        Assert.AreEqual((uint)(package.Length - 4), ReadLength(package, 0));
        var entries = new List<(string, byte[])>();
        var at = 4;
        while (at < package.Length)
        {
            var length = (int)ReadLength(package, at);
            at += 4;
            var end = Array.IndexOf(package, (byte)0, at);
            var name = Encoding.ASCII.GetString(package, at, end - at);
            at = end + 1;
            entries.Add((name, package.Skip(at).Take(length).ToArray()));
            at += length;
        }
        return entries;
    }

    private static uint ReadLength(byte[] bytes, int at) =>
        (uint)(bytes[at] | bytes[at + 1] << 8 | bytes[at + 2] << 16 | bytes[at + 3] << 24);

    private string TurboCdFolder()
    {
        var folder = Path.Combine(_root, "cd");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "game.hcd"), "1,AUDIO,track01.ogg\r\n2,DATA,data.bin\r\n3,DATA,missing.bin\r\n");
        File.WriteAllText(Path.Combine(folder, "track01.ogg"), "ogg1");
        File.WriteAllText(Path.Combine(folder, "data.bin"), "bin1");
        return folder;
    }
}
