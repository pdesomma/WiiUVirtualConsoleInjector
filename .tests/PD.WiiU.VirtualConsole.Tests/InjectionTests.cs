using PD.WiiU.VirtualConsole.Options;

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

    private static BaseTitle N64Base() =>
        new(new TitleId(TitleType.Game, 0x101C9300), "Donkey Kong 64", Region.UnitedStates, SourceConsole.N64);

    private static Rom N64Rom() => new(@"C:\roms\game.z64", SourceConsole.N64);

    private static Game NewGame() => new(
        new TitleId(TitleType.Demo, 0x12345678),
        new GroupId(0x5678),
        new ProductCode(ProductCode.EShop, "FAAE"));
}
