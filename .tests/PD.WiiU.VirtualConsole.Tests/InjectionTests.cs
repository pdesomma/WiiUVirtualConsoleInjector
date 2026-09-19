using PD.WiiU.VirtualConsole.Options;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class InjectionTests
{
    [TestMethod]
    public void ConsoleComesFromTheBase()
    {
        var injection = new Injection(N64Base(), N64Rom(), NewGame());

        Assert.AreEqual(SourceConsole.N64, injection.Console);
    }

    [TestMethod]
    public void Constructor_BaseTemplate_SetsBaseAndLeavesCoreNull()
    {
        var @base = N64Base();

        var injection = new Injection(@base, N64Rom(), NewGame());

        Assert.AreSame(@base, injection.Template);
        Assert.AreSame(@base, injection.Base);
        Assert.IsNull(injection.Core);
    }

    [TestMethod]
    public void Constructor_CoreTemplate_SetsCoreAndLeavesBaseNull()
    {
        var core = GenesisCore();

        var injection = new Injection(core, GenesisRom(), NewGame());

        Assert.AreSame(core, injection.Template);
        Assert.AreSame(core, injection.Core);
        Assert.IsNull(injection.Base);
        Assert.AreEqual(SourceConsole.Genesis, injection.Console);
    }

    [TestMethod]
    public void Constructor_RomForAnotherConsoleThanTheCore_MessageSaysCore()
    {
        var error = Assert.ThrowsExactly<ArgumentException>(() => new Injection(GenesisCore(), N64Rom(), NewGame()));

        StringAssert.Contains(error.Message, "core is for Genesis");
    }

    [TestMethod]
    public void DefaultsToNoArtworkSoundOrOptions()
    {
        var injection = new Injection(N64Base(), N64Rom(), NewGame());

        Assert.AreSame(Artwork.None, injection.Artwork);
        Assert.IsNull(injection.BootSoundPath);
        Assert.IsNull(injection.Options);
    }

    [TestMethod]
    public void RejectsNullParts()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new Injection(null!, N64Rom(), NewGame()));
        Assert.ThrowsExactly<ArgumentNullException>(() => new Injection(N64Base(), null!, NewGame()));
        Assert.ThrowsExactly<ArgumentNullException>(() => new Injection(N64Base(), N64Rom(), null!));
    }

    [TestMethod]
    public void RejectsOptionsForAnotherConsole()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            new Injection(N64Base(), N64Rom(), NewGame()) { Options = new GbaOptions() });
    }

    [TestMethod]
    public void RejectsRomForAnotherConsole()
    {
        var rom = new Rom(@"C:\roms\game.gba", SourceConsole.Gba);

        Assert.ThrowsExactly<ArgumentException>(() => new Injection(N64Base(), rom, NewGame()));
    }

    [TestMethod]
    public void TakesOptionsForItsConsole()
    {
        var options = new N64Options { WideScreen = true };
        var injection = new Injection(N64Base(), N64Rom(), NewGame()) { Options = options };

        Assert.AreSame(options, injection.Options);
    }

    private static RetroArchCore GenesisCore() =>
        new("genesis_plus_gx", "Genesis Plus GX", SourceConsole.Genesis, "Accurate.");

    private static Rom GenesisRom() => new(@"C:\roms\game.md", SourceConsole.Genesis);

    private static BaseTitle N64Base() =>
        new(new TitleId(TitleType.Game, 0x101C9300), "Donkey Kong 64", Region.UnitedStates, SourceConsole.N64);

    private static Rom N64Rom() => new(@"C:\roms\game.z64", SourceConsole.N64);

    private static Game NewGame() => new(
        new TitleId(TitleType.Demo, 0x12345678),
        new GroupId(0x5678),
        new ProductCode(ProductCode.EShop, "FAAE"));
}
