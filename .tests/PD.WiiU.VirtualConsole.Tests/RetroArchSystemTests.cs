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
        var system = new RetroArchSystem(SourceConsole.AtariLynx, ".lnx") { BiosFiles = new[] { new BiosFile("lynxboot.img"), new BiosFile("extra.bin") } };
        try
        {
            Directory.CreateDirectory(sd);
            CollectionAssert.AreEqual(new[] { "lynxboot.img", "extra.bin" }, system.MissingBios(sd).Select(b => b.Label).ToArray(), "nothing on the card yet");

            var folder = Path.Combine(sd, "retroarch", "system");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, "lynxboot.img"), new byte[] { 1 });

            CollectionAssert.AreEqual(new[] { "extra.bin" }, system.MissingBios(sd).Select(b => b.Label).ToArray());
            Assert.AreEqual(0, new RetroArchSystem(SourceConsole.Genesis, ".md").MissingBios(sd).Count, "no BIOS needed, nothing missing");
        }
        finally
        {
            Directory.Delete(sd, recursive: true);
        }
    }

    [TestMethod]
    public void MissingBios_SecondAcceptedNamePresent_IsSatisfied()
    {
        var sd = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Tests", Guid.NewGuid().ToString("N"));
        var bios = new BiosFile("PlayStation BIOS", "scph5501.bin", "scph1001.bin");
        var system = new RetroArchSystem(SourceConsole.PlayStation, ".cue") { BiosFiles = new[] { bios } };
        try
        {
            var folder = Path.Combine(sd, "retroarch", "system");
            Directory.CreateDirectory(folder);
            Assert.AreSame(bios, system.MissingBios(sd).Single(), "neither name on the card");

            File.WriteAllBytes(Path.Combine(folder, "scph1001.bin"), new byte[] { 1 });

            Assert.AreEqual(0, system.MissingBios(sd).Count, "the second name satisfies it");
        }
        finally
        {
            Directory.Delete(sd, recursive: true);
        }
    }

    [TestMethod]
    public void MissingBios_SubfolderNamePresent_IsSatisfied()
    {
        var sd = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Tests", Guid.NewGuid().ToString("N"));
        var bios = new BiosFile("Neo Geo CD BIOS", "neocd/neocd_z.rom", "neocd/neocd_f.rom");
        var system = new RetroArchSystem(SourceConsole.NeoGeoCd, ".cue") { BiosFiles = new[] { bios } };
        try
        {
            var folder = Path.Combine(sd, "retroarch", "system");
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, "neocd_z.rom"), new byte[] { 1 });
            Assert.AreSame(bios, system.MissingBios(sd).Single(), "at the system root, not under neocd/");

            Directory.CreateDirectory(Path.Combine(folder, "neocd"));
            File.WriteAllBytes(Path.Combine(folder, "neocd", "neocd_z.rom"), new byte[] { 1 });

            Assert.AreEqual(0, system.MissingBios(sd).Count, "the subfolder name satisfies it");
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
