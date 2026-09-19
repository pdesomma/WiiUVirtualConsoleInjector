using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.RetroArch;
using WiiUVirtualConsoleInjector.Assets;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class ConsoleIconsTests
{
    [TestMethod]
    public void Caption_Genesis_SaysAromaOnly()
    {
        Assert.AreEqual("Aroma only", ConsoleIcons.Caption(SourceConsole.Genesis));
        Assert.AreEqual("Aroma only", ConsoleIcons.Caption(SourceConsole.Arcade));
        Assert.AreEqual("Aroma only", ConsoleIcons.Caption(SourceConsole.NeoGeo));
        Assert.AreEqual("Aroma only", ConsoleIcons.Caption(SourceConsole.PlayStation));
    }

    [TestMethod]
    public void Caption_BaseConsoles_Null()
    {
        var cores = new EmbeddedRetroArchCores();
        foreach (var console in Enum.GetValues<SourceConsole>())
            Assert.AreEqual(cores.System(console) is null ? null : "Aroma only", ConsoleIcons.Caption(console), console.ToString());
    }

    [TestMethod]
    public void DisplayName_EveryConsole_HasALabel()
    {
        Assert.AreEqual("Sega Genesis", ConsoleIcons.DisplayName(SourceConsole.Genesis));
        Assert.AreEqual("NES", ConsoleIcons.DisplayName(SourceConsole.Nes));
        Assert.AreEqual("Arcade", ConsoleIcons.DisplayName(SourceConsole.Arcade));
        Assert.AreEqual("Neo Geo", ConsoleIcons.DisplayName(SourceConsole.NeoGeo));
        Assert.AreEqual("PlayStation", ConsoleIcons.DisplayName(SourceConsole.PlayStation));
        foreach (var console in Enum.GetValues<SourceConsole>())
            Assert.IsFalse(string.IsNullOrWhiteSpace(ConsoleIcons.DisplayName(console)), console.ToString());
    }

    [TestMethod]
    public void UriFor_Genesis_PointsAtBothLogos()
    {
        StringAssert.EndsWith(ConsoleIcons.UriFor(SourceConsole.Genesis).ToString(), "Assets/Consoles/Genesis.png");
        StringAssert.EndsWith(ConsoleIcons.DarkUriFor(SourceConsole.Genesis).ToString(), "Assets/Consoles/Dark/Genesis.png");
    }

    [TestMethod]
    public void UriFor_EveryConsole_HasBothLogosOnDisk()
    {
        var root = Path.Combine(FindRepoRoot(), "WiiUVirtualConsoleInjector", "Assets", "Consoles");
        foreach (var console in Enum.GetValues<SourceConsole>())
        {
            Assert.IsTrue(File.Exists(Path.Combine(root, console + ".png")), console + " white logo");
            Assert.IsTrue(File.Exists(Path.Combine(root, "Dark", console + ".png")), console + " dark logo");
        }
    }

    private static string FindRepoRoot()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "README.md")))
            folder = folder.Parent;
        Assert.IsNotNull(folder, "repo root");
        return folder.FullName;
    }
}
