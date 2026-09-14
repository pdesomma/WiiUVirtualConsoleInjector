using PD.WiiU.VirtualConsole.Options;

namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class OptionsTests
{
    [TestMethod]
    public void EachOptionsTypeNamesItsConsole()
    {
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
