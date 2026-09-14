using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Retro.Tests;

[TestClass]
public class MsxTests
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
    public void Constructor_Default_UsesKnownHeaderLength()
    {
        Assert.AreEqual(0x580B3, new MsxRomInjector().HeaderLength);
        Assert.AreEqual(SourceConsole.Msx, new MsxRomInjector().Console);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new MsxRomInjector(0));
    }

    [TestMethod]
    public async Task InjectAsync_Rom_KeepsHeaderAndReplacesTheRestWithTheWholeRom()
    {
        var title = TitleDirectory.Create(Path.Combine(_root, "title"));
        var header = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();
        var package = Path.Combine(title.Content, "msx", "msx.pkg");
        Directory.CreateDirectory(Path.GetDirectoryName(package)!);
        File.WriteAllBytes(package, header.Concat(new byte[500]).ToArray());
        var rom = Enumerable.Range(0, 37).Select(i => (byte)(0xA0 + i)).ToArray();
        var romPath = Path.Combine(_root, "game.rom");
        File.WriteAllBytes(romPath, rom);

        await new MsxRomInjector(headerLength: 100).InjectAsync(Injection(romPath), title);

        CollectionAssert.AreEqual(header.Concat(rom).ToArray(), File.ReadAllBytes(package));
    }

    [TestMethod]
    public async Task InjectAsync_PackageShorterThanHeader_ThrowsInvalidDataException()
    {
        var title = TitleDirectory.Create(Path.Combine(_root, "title"));
        var package = Path.Combine(title.Content, "msx", "msx.pkg");
        Directory.CreateDirectory(Path.GetDirectoryName(package)!);
        File.WriteAllBytes(package, new byte[10]);
        var romPath = Path.Combine(_root, "game.rom");
        File.WriteAllBytes(romPath, new byte[4]);

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => new MsxRomInjector(headerLength: 100).InjectAsync(Injection(romPath), title));
    }

    [TestMethod]
    public async Task InjectAsync_NoPackage_ThrowsFileNotFoundException()
    {
        var title = TitleDirectory.Create(Path.Combine(_root, "title"));

        await Assert.ThrowsExactlyAsync<FileNotFoundException>(() => new MsxRomInjector().InjectAsync(Injection("x.rom"), title));
    }

    [TestMethod]
    public async Task InjectAsync_WrongConsoleOrNulls_Throw()
    {
        var title = TitleDirectory.Create(Path.Combine(_root, "title"));
        var nes = new Injection(new BaseTitle(new TitleId(TitleType.Game, 1), "NES", Region.UnitedStates, SourceConsole.Nes), new Rom("x.nes", SourceConsole.Nes), Game());

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => new MsxRomInjector().InjectAsync(nes, title));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => new MsxRomInjector().InjectAsync(null!, title));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => new MsxRomInjector().InjectAsync(Injection("x.rom"), null!));
    }

    private static Game Game() =>
        new(new TitleId(TitleType.Demo, 0x1ABCDE00), GroupId.Parse("00001ABC"), ProductCode.Parse("WUP-N-TEST"));

    private static Injection Injection(string rom) =>
        new(new BaseTitle(new TitleId(TitleType.Game, 0x10101D00), "Base", Region.UnitedStates, SourceConsole.Msx), new Rom(rom, SourceConsole.Msx), Game());
}
