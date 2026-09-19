namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public sealed class RetroArchSystemTests
{
    [TestMethod]
    public void Accepts_ExtensionCaseAndUnknown()
    {
        var system = new RetroArchSystem(SourceConsole.MasterSystem, ".sms");

        Assert.IsTrue(system.Accepts(@"C:\roms\Sonic.SMS"));
        Assert.IsFalse(system.Accepts(@"C:\roms\Sonic.gg"));
        Assert.IsFalse(system.Accepts("noextension"));
    }

    [TestMethod]
    public void Accepts_NullPath_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new RetroArchSystem(SourceConsole.GameGear, ".gg").Accepts(null!));
    }

    [TestMethod]
    public void Constructor_InvalidExtensions_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new RetroArchSystem(SourceConsole.Genesis, null!));
        Assert.ThrowsExactly<ArgumentException>(() => new RetroArchSystem(SourceConsole.Genesis));
        Assert.ThrowsExactly<ArgumentException>(() => new RetroArchSystem(SourceConsole.Genesis, "md"));
        Assert.ThrowsExactly<ArgumentException>(() => new RetroArchSystem(SourceConsole.Genesis, "."));
        Assert.ThrowsExactly<ArgumentException>(() => new RetroArchSystem(SourceConsole.Genesis, ".md", " "));
    }

    [TestMethod]
    public void Constructor_ValidExtensions_KeepsThemLowerCase()
    {
        var system = new RetroArchSystem(SourceConsole.Sega32X, ".32X", ".bin");

        Assert.AreEqual(SourceConsole.Sega32X, system.Console);
        CollectionAssert.AreEqual(new[] { ".32x", ".bin" }, system.Extensions.ToArray());
    }
}
