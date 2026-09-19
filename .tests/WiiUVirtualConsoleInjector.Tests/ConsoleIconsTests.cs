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
    public void Caption_NewRetroArchConsoles_SayAromaOnly()
    {
        foreach (var console in new[] { SourceConsole.PokemonMini, SourceConsole.NeoGeoPocket, SourceConsole.WonderSwan, SourceConsole.Supervision, SourceConsole.GameAndWatch, SourceConsole.ColecoVision, SourceConsole.Intellivision, SourceConsole.Odyssey2, SourceConsole.Vectrex, SourceConsole.SegaCd, SourceConsole.NeoGeoCd })
            Assert.AreEqual("Aroma only", ConsoleIcons.Caption(console), console.ToString());
    }

    [TestMethod]
    public void Caption_Computers_SayAromaOnly()
    {
        foreach (var console in new[] { SourceConsole.Dos, SourceConsole.Commodore64, SourceConsole.Commodore128, SourceConsole.AmstradCpc, SourceConsole.ZxSpectrum, SourceConsole.AtariSt })
            Assert.AreEqual("Aroma only", ConsoleIcons.Caption(console), console.ToString());
    }

    [TestMethod]
    public void Caption_AromaOnly_ExactlyWhenTheConsoleHasCoresAndNoBase()
    {
        var cores = new EmbeddedRetroArchCores();
        var catalog = BaseCatalog.Bundled();
        foreach (var console in Enum.GetValues<SourceConsole>())
        {
            var coreOnly = cores.System(console) is not null && catalog.For(console).Count == 0;
            Assert.AreEqual(coreOnly ? "Aroma only" : null, ConsoleIcons.Caption(console), console.ToString());
        }
    }

    [TestMethod]
    public void Caption_ConsolesWithABase_Null()
    {
        foreach (var console in new[] { SourceConsole.Nes, SourceConsole.Snes, SourceConsole.Gba, SourceConsole.GameBoy, SourceConsole.Tg16, SourceConsole.Msx })
            Assert.IsNull(ConsoleIcons.Caption(console), console + " can still be built on its base");
    }

    [TestMethod]
    public void DisplayName_EveryConsole_HasALabel()
    {
        Assert.AreEqual("Sega Genesis", ConsoleIcons.DisplayName(SourceConsole.Genesis));
        Assert.AreEqual("NES", ConsoleIcons.DisplayName(SourceConsole.Nes));
        Assert.AreEqual("Game Boy / Color", ConsoleIcons.DisplayName(SourceConsole.GameBoy));
        Assert.AreEqual("Game Boy Advance", ConsoleIcons.DisplayName(SourceConsole.Gba));
        Assert.AreEqual("Arcade", ConsoleIcons.DisplayName(SourceConsole.Arcade));
        Assert.AreEqual("Neo Geo", ConsoleIcons.DisplayName(SourceConsole.NeoGeo));
        Assert.AreEqual("PlayStation", ConsoleIcons.DisplayName(SourceConsole.PlayStation));
        foreach (var console in Enum.GetValues<SourceConsole>())
            Assert.IsFalse(string.IsNullOrWhiteSpace(ConsoleIcons.DisplayName(console)), console.ToString());
    }

    [TestMethod]
    public void DisplayName_NewRetroArchConsoles_UseTheirMarketNames()
    {
        Assert.AreEqual("Pokémon Mini", ConsoleIcons.DisplayName(SourceConsole.PokemonMini));
        Assert.AreEqual("Neo Geo Pocket", ConsoleIcons.DisplayName(SourceConsole.NeoGeoPocket));
        Assert.AreEqual("WonderSwan", ConsoleIcons.DisplayName(SourceConsole.WonderSwan));
        Assert.AreEqual("Watara Supervision", ConsoleIcons.DisplayName(SourceConsole.Supervision));
        Assert.AreEqual("Game & Watch", ConsoleIcons.DisplayName(SourceConsole.GameAndWatch));
        Assert.AreEqual("ColecoVision", ConsoleIcons.DisplayName(SourceConsole.ColecoVision));
        Assert.AreEqual("Intellivision", ConsoleIcons.DisplayName(SourceConsole.Intellivision));
        Assert.AreEqual("Odyssey²", ConsoleIcons.DisplayName(SourceConsole.Odyssey2));
        Assert.AreEqual("Vectrex", ConsoleIcons.DisplayName(SourceConsole.Vectrex));
    }

    [TestMethod]
    public void DisplayName_DiscConsoles_UseTheirMarketNames()
    {
        Assert.AreEqual("Sega CD", ConsoleIcons.DisplayName(SourceConsole.SegaCd));
        Assert.AreEqual("Neo Geo CD", ConsoleIcons.DisplayName(SourceConsole.NeoGeoCd));
    }

    [TestMethod]
    public void DisplayName_Computers_UseTheirMarketNames()
    {
        Assert.AreEqual("DOS", ConsoleIcons.DisplayName(SourceConsole.Dos));
        Assert.AreEqual("Commodore 64", ConsoleIcons.DisplayName(SourceConsole.Commodore64));
        Assert.AreEqual("Commodore 128", ConsoleIcons.DisplayName(SourceConsole.Commodore128));
        Assert.AreEqual("Amstrad CPC", ConsoleIcons.DisplayName(SourceConsole.AmstradCpc));
        Assert.AreEqual("ZX Spectrum", ConsoleIcons.DisplayName(SourceConsole.ZxSpectrum));
        Assert.AreEqual("Atari ST", ConsoleIcons.DisplayName(SourceConsole.AtariSt));
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

    [TestMethod]
    public void UriFor_EveryCompanyTile_HasBothLogosOnDisk()
    {
        var root = Path.Combine(FindRepoRoot(), "WiiUVirtualConsoleInjector", "Assets", "Consoles");
        foreach (var tile in ConsoleGroups.Top.Where(t => t.IsGroup))
        {
            Assert.IsTrue(File.Exists(Path.Combine(root, tile.IconName + ".png")), tile.Label + " white logo");
            Assert.IsTrue(File.Exists(Path.Combine(root, "Dark", tile.IconName + ".png")), tile.Label + " dark logo");
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
