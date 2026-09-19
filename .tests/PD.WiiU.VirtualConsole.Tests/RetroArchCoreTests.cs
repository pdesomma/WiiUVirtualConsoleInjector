namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class RetroArchCoreTests
{
    [TestMethod]
    public void Constructor_BlankId_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new RetroArchCore(null!, "Name", SourceConsole.Genesis, "Desc"));
        Assert.ThrowsExactly<ArgumentException>(() => new RetroArchCore("", "Name", SourceConsole.Genesis, "Desc"));
        Assert.ThrowsExactly<ArgumentException>(() => new RetroArchCore("   ", "Name", SourceConsole.Genesis, "Desc"));
    }

    [TestMethod]
    public void Constructor_IdWithDashOrSpace_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new RetroArchCore("genesis-plus-gx", "Name", SourceConsole.Genesis, "Desc"));
        Assert.ThrowsExactly<ArgumentException>(() => new RetroArchCore("genesis plus", "Name", SourceConsole.Genesis, "Desc"));
        Assert.ThrowsExactly<ArgumentException>(() => new RetroArchCore("core.rpx", "Name", SourceConsole.Genesis, "Desc"));
    }

    [TestMethod]
    public void Constructor_NullDescription_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new RetroArchCore("genesis_plus_gx", "Name", SourceConsole.Genesis, null!));
    }

    [TestMethod]
    public void Constructor_NullOrBlankName_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new RetroArchCore("genesis_plus_gx", null!, SourceConsole.Genesis, "Desc"));
        Assert.ThrowsExactly<ArgumentException>(() => new RetroArchCore("genesis_plus_gx", " ", SourceConsole.Genesis, "Desc"));
    }

    [TestMethod]
    public void Constructor_ValidArguments_ExposesThem()
    {
        var core = new RetroArchCore("genesis_plus_gx", "Genesis Plus GX", SourceConsole.Genesis, "Accurate.");

        Assert.AreEqual("genesis_plus_gx", core.Id);
        Assert.AreEqual("Genesis Plus GX", core.Name);
        Assert.AreEqual(SourceConsole.Genesis, core.Console);
        Assert.AreEqual("Accurate.", core.Description);
    }

    [TestMethod]
    public void IsRecommended_DefaultsFalseAndTakesInit()
    {
        Assert.IsFalse(new RetroArchCore("picodrive", "PicoDrive", SourceConsole.Genesis, "Fast.").IsRecommended);
        Assert.IsTrue(new RetroArchCore("genesis_plus_gx", "Genesis Plus GX", SourceConsole.Genesis, "Accurate.") { IsRecommended = true }.IsRecommended);
    }

    [TestMethod]
    public void RpxFileName_IsIdWithTheLibretroSuffix()
    {
        var core = new RetroArchCore("genesis_plus_gx", "Genesis Plus GX", SourceConsole.Genesis, "Accurate.");

        Assert.AreEqual("genesis_plus_gx_libretro.rpx", core.RpxFileName);
        Assert.AreEqual("_libretro.rpx", RetroArchCore.RpxSuffix);
    }

    [TestMethod]
    public void ToString_IsTheName()
    {
        Assert.AreEqual("Genesis Plus GX", new RetroArchCore("genesis_plus_gx", "Genesis Plus GX", SourceConsole.Genesis, "Accurate.").ToString());
    }
}
