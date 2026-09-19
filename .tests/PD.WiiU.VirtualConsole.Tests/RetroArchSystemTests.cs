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
    public void MissingBios_BlankRoot_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new RetroArchSystem(SourceConsole.AtariLynx, ".lnx").MissingBios(" "));
    }

    [TestMethod]
    public void MissingBios_FilesPresentOrAbsent_ListsTheAbsentOnes()
    {
        var sd = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Tests", Guid.NewGuid().ToString("N"));
        var system = new RetroArchSystem(SourceConsole.AtariLynx, ".lnx") { BiosFiles = new[] { "lynxboot.img", "extra.bin" } };
        try
        {
            Directory.CreateDirectory(sd);
            CollectionAssert.AreEqual(new[] { "lynxboot.img", "extra.bin" }, system.MissingBios(sd).ToArray(), "nothing on the card yet");

            var folder = Path.Combine(sd, "retroarch", "system");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, "lynxboot.img"), new byte[] { 1 });

            CollectionAssert.AreEqual(new[] { "extra.bin" }, system.MissingBios(sd).ToArray());
            Assert.AreEqual(0, new RetroArchSystem(SourceConsole.Genesis, ".md").MissingBios(sd).Count, "no BIOS needed, nothing missing");
        }
        finally
        {
            Directory.Delete(sd, recursive: true);
        }
    }

    [TestMethod]
    public void Constructor_ValidExtensions_KeepsThemLowerCase()
    {
        var system = new RetroArchSystem(SourceConsole.Sega32X, ".32X", ".bin");

        Assert.AreEqual(SourceConsole.Sega32X, system.Console);
        CollectionAssert.AreEqual(new[] { ".32x", ".bin" }, system.Extensions.ToArray());
    }
}
