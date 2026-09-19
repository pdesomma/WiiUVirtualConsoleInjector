using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.RetroArch;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class UwuvciCompatibilityListsTests
{
    [TestMethod]
    public void For_EveryConsole_PointsAtItsPage()
    {
        var lists = new UwuvciCompatibilityLists(new FakeLinkOpener());

        Assert.AreEqual("https://uwuvci-prime.github.io/UWUVCI-Resources/n64/n64.html", lists.For(SourceConsole.N64).ToString());
        Assert.AreEqual("https://uwuvci-prime.github.io/UWUVCI-Resources/tgfx/tgfx.html", lists.For(SourceConsole.Tg16).ToString(), "the site calls TurboGrafx tgfx");
        Assert.AreEqual("https://uwuvci-prime.github.io/UWUVCI-Resources/gcn/gcn.html", lists.For(SourceConsole.GameCube).ToString());
        // UWUVCI never injected the RetroArch consoles, so its site has no pages for them
        var cores = new EmbeddedRetroArchCores();
        foreach (var console in Enum.GetValues<SourceConsole>().Where(c => cores.System(c) is null))
            StringAssert.StartsWith(lists.For(console).ToString(), UwuvciCompatibilityLists.Root, console.ToString());
    }

    [TestMethod]
    public void For_Genesis_ThrowsArgumentOutOfRangeException()
    {
        var lists = new UwuvciCompatibilityLists(new FakeLinkOpener());

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => lists.For(SourceConsole.Genesis));
    }

    [TestMethod]
    public async Task OpenAsync_OpensThePageThroughTheLinkOpener()
    {
        var links = new FakeLinkOpener();

        var opened = await new UwuvciCompatibilityLists(links).OpenAsync(SourceConsole.Msx);

        Assert.IsTrue(opened);
        CollectionAssert.AreEqual(new[] { new Uri("https://uwuvci-prime.github.io/UWUVCI-Resources/msx/msx.html") }, links.Opened);
    }

    [TestMethod]
    public void Constructor_NullLinks_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new UwuvciCompatibilityLists(null!));
    }
}
