using PD.WiiU.VirtualConsole;
using WiiUVirtualConsoleInjector.Assets;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class ConsoleIconsTests
{
    [TestMethod]
    public void Caption_Genesis_SaysAromaOnly()
    {
        Assert.AreEqual("Aroma only", ConsoleIcons.Caption(SourceConsole.Genesis));
    }

    [TestMethod]
    public void Caption_BaseConsoles_Null()
    {
        foreach (var console in Enum.GetValues<SourceConsole>().Where(c => c != SourceConsole.Genesis))
            Assert.IsNull(ConsoleIcons.Caption(console), console.ToString());
    }

    [TestMethod]
    public void DisplayName_EveryConsole_HasALabel()
    {
        Assert.AreEqual("Sega Genesis", ConsoleIcons.DisplayName(SourceConsole.Genesis));
        Assert.AreEqual("NES", ConsoleIcons.DisplayName(SourceConsole.Nes));
        foreach (var console in Enum.GetValues<SourceConsole>())
            Assert.IsFalse(string.IsNullOrWhiteSpace(ConsoleIcons.DisplayName(console)), console.ToString());
    }

    [TestMethod]
    public void UriFor_Genesis_PointsAtBothLogos()
    {
        StringAssert.EndsWith(ConsoleIcons.UriFor(SourceConsole.Genesis).ToString(), "Assets/Consoles/Genesis.png");
        StringAssert.EndsWith(ConsoleIcons.DarkUriFor(SourceConsole.Genesis).ToString(), "Assets/Consoles/Dark/Genesis.png");
    }
}
