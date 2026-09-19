using PD.WiiU.VirtualConsole.Options;

namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class OptionsTests
{
    [TestMethod]
    public void ArcadeDefaultsToArcadeWithNoCompanions()
    {
        var options = new ArcadeOptions();

        Assert.AreEqual(SourceConsole.Arcade, options.Console);
        Assert.AreEqual(0, options.CompanionPaths.Count);
    }

    [TestMethod]
    public void ArcadeTakesNeoGeoAndCompanionsOnInit()
    {
        var options = new ArcadeOptions { Console = SourceConsole.NeoGeo, CompanionPaths = new[] { @"C:\roms\neogeo.zip" } };

        Assert.AreEqual(SourceConsole.NeoGeo, options.Console);
        CollectionAssert.AreEqual(new[] { @"C:\roms\neogeo.zip" }, options.CompanionPaths.ToArray());
    }

    [TestMethod]
    public void GbaTakesGameBoyOnInit()
    {
        var options = new GbaOptions { Console = SourceConsole.GameBoy, RemoveDarkFilter = true };

        Assert.AreEqual(SourceConsole.GameBoy, options.Console);
        Assert.IsTrue(options.RemoveDarkFilter);
        Assert.AreEqual(SourceConsole.Gba, new GbaOptions().Console);
    }

    [TestMethod]
    public void EachOptionsTypeNamesItsConsole()
    {
        Assert.AreEqual(SourceConsole.Arcade, new ArcadeOptions().Console);
        Assert.AreEqual(SourceConsole.Gba, new GbaOptions().Console);
        Assert.AreEqual(SourceConsole.GameCube, new GameCubeOptions().Console);
        Assert.AreEqual(SourceConsole.N64, new N64Options().Console);
        Assert.AreEqual(SourceConsole.Nes, new NesOptions().Console);
        Assert.AreEqual(SourceConsole.Snes, new SnesOptions().Console);
        Assert.AreEqual(SourceConsole.Wii, new WiiOptions().Console);
    }

    [TestMethod]
    public void RomRequiresAPath()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new Rom("", SourceConsole.Nes));
        Assert.ThrowsExactly<ArgumentException>(() => new Rom("  ", SourceConsole.Nes));
    }

    [TestMethod]
    public void WiiDefaultsMatchTheStockInjection()
    {
        var options = new WiiOptions();

        Assert.AreEqual(WiiControllerMode.ClassicController, options.ControllerMode);
        Assert.IsTrue(options.Passthrough);
        Assert.IsTrue(options.TrimDisc);
        Assert.IsNull(options.TargetRegion);
    }
}
