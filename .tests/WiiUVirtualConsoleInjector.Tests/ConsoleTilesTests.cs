using PD.WiiU.VirtualConsole;
using WiiUSharp;
using WiiUVirtualConsoleInjector.Assets;
using WiiUVirtualConsoleInjector.Services;
using WiiUVirtualConsoleInjector.ViewModels;
using static WiiUVirtualConsoleInjector.Tests.InjectFakes;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class ConsoleTilesTests
{
    private InjectDialogService _dialogs = null!;
    private InjectSettingsService _settings = null!;

    [TestInitialize]
    public void Setup()
    {
        SynchronizationContext.SetSynchronizationContext(new InlineSynchronizationContext());
        _dialogs = new InjectDialogService();
        _settings = new InjectSettingsService();
    }

    [TestMethod]
    public void CloseGroup_WhenOpen_ReturnsToTheTopLevel()
    {
        var vm = Create();
        vm.SelectedTopTile = vm.TopTiles.First(t => t.Label == ConsoleGroups.Sega);

        vm.CloseGroupCommand.Execute(null);

        Assert.IsFalse(vm.IsGroupOpen);
        Assert.AreEqual(0, vm.Tiles.Count);
        Assert.IsNull(vm.SelectedTopTile);
    }

    [TestMethod]
    public void ConsoleGroups_Top_CoversEveryConsoleOnce()
    {
        var consoles = ConsoleGroups.Top.SelectMany(t => t.Consoles).ToArray();

        CollectionAssert.AreEquivalent(Enum.GetValues<SourceConsole>(), consoles);
        Assert.AreEqual(consoles.Length, consoles.Distinct().Count());
        Assert.AreEqual(ConsoleGroups.Nintendo, ConsoleGroups.GroupOf(SourceConsole.Nes)!.Label);
        Assert.AreEqual(ConsoleGroups.Sega, ConsoleGroups.GroupOf(SourceConsole.GameGear)!.Label);
        Assert.IsNull(ConsoleGroups.GroupOf(SourceConsole.Msx));
        Assert.IsNull(ConsoleGroups.GroupOf(SourceConsole.PlayStation));
        Assert.IsNull(ConsoleGroups.GroupOf(SourceConsole.Arcade));
        Assert.IsNull(ConsoleGroups.GroupOf(SourceConsole.NeoGeo));
        Assert.AreEqual(SourceConsole.PlayStation, ConsoleGroups.Top[^3].Console, "PlayStation sits before the arcade tiles");
        Assert.AreEqual(SourceConsole.NeoGeo, ConsoleGroups.Top[^1].Console, "arcade tiles close the top level");
    }

    [TestMethod]
    public void ConsoleGroups_Members_RequiresACompany()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => ConsoleGroups.Members(null!));
        Assert.ThrowsExactly<ArgumentException>(() => ConsoleGroups.Members(new ConsoleTile(SourceConsole.Msx)));
        Assert.AreEqual(4, ConsoleGroups.Members(ConsoleGroups.Top.First(t => t.Label == ConsoleGroups.Sega)).Count);
    }

    [TestMethod]
    public void ConsoleTile_Company_CarriesTheCaptionAllItsConsolesShare()
    {
        Assert.AreEqual("Aroma only", new ConsoleTile("Sega", new[] { SourceConsole.Genesis, SourceConsole.GameGear }).Caption);
        Assert.AreEqual("Aroma only", ConsoleGroups.Top.First(t => t.Label == ConsoleGroups.Atari).Caption);
        Assert.IsNull(new ConsoleTile("Mixed", new[] { SourceConsole.Nes, SourceConsole.VirtualBoy }).Caption, "one plain console drops the note");
        Assert.IsNull(ConsoleGroups.Top.First(t => t.Label == ConsoleGroups.Nintendo).Caption);
    }

    [TestMethod]
    public void ConsoleTile_Company_ValidatesItsArguments()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new ConsoleTile(" ", new[] { SourceConsole.Nes }));
        Assert.ThrowsExactly<ArgumentNullException>(() => new ConsoleTile("Nintendo", null!));
        Assert.ThrowsExactly<ArgumentException>(() => new ConsoleTile("Nintendo", Array.Empty<SourceConsole>()));
    }

    [TestMethod]
    public void ConsoleTile_Console_CarriesLabelCaptionAndIcon()
    {
        var tile = new ConsoleTile(SourceConsole.Genesis);

        Assert.IsFalse(tile.IsGroup);
        Assert.AreEqual(SourceConsole.Genesis, tile.Console);
        Assert.AreEqual("Sega Genesis", tile.Label);
        Assert.AreEqual("Aroma only", tile.Caption);
        Assert.AreEqual("Genesis", tile.IconName);
        Assert.IsNull(new ConsoleTile(SourceConsole.Nes).Caption);
        Assert.AreEqual("PlayStation", new ConsoleTile(SourceConsole.PlayStation).Label);
        Assert.AreEqual("Aroma only", new ConsoleTile(SourceConsole.PlayStation).Caption);
    }

    [TestMethod]
    public void Load_ConsoleInsideACompany_OpensThatCompanyWithItHighlighted()
    {
        var vm = Create();

        vm.Load(RecordFor(SourceConsole.GameGear));

        Assert.AreEqual(ConsoleGroups.Sega, vm.OpenGroupName);
        Assert.AreEqual(SourceConsole.GameGear, vm.SelectedTile?.Console);
        Assert.AreEqual(SourceConsole.GameGear, vm.SelectedConsole);
    }

    [TestMethod]
    public void SelectedTile_Company_OpensItsConsoles()
    {
        var vm = Create();

        vm.SelectedTopTile = vm.TopTiles.First(t => t.Label == ConsoleGroups.Nintendo);

        Assert.IsTrue(vm.IsGroupOpen);
        Assert.AreEqual(ConsoleGroups.Nintendo, vm.OpenGroupName);
        Assert.AreEqual(ConsoleGroups.Top.First(t => t.Label == ConsoleGroups.Nintendo).Consoles.Count, vm.Tiles.Count);
        Assert.IsTrue(vm.Tiles.All(t => !t.IsGroup));
        Assert.IsNull(vm.SelectedTile, "nothing is highlighted while browsing, so a click on any console counts");
        Assert.AreEqual(1, vm.Step, "opening a company is not choosing a console");
    }

    [TestMethod]
    public void SelectedTile_ConsoleAlreadySelected_StillAdvances()
    {
        var vm = Create();
        vm.SelectedTopTile = vm.TopTiles.First(t => t.Label == ConsoleGroups.Nintendo);

        vm.SelectedTile = vm.Tiles.First(t => t.Console == SourceConsole.Nes);

        Assert.AreEqual(2, vm.Step);
    }

    [TestMethod]
    public void SelectedTile_LooseConsole_SelectsItAndAdvances()
    {
        var vm = Create();

        vm.SelectedTopTile = vm.TopTiles.First(t => t.Console == SourceConsole.Tg16);

        Assert.AreEqual(SourceConsole.Tg16, vm.SelectedConsole);
        Assert.AreEqual(2, vm.Step);
        Assert.IsFalse(vm.IsGroupOpen);
    }

    [TestMethod]
    public void StartOver_Always_ShowsTheTopLevelWithNothingHighlighted()
    {
        var vm = Create();
        vm.SelectedTopTile = vm.TopTiles.First(t => t.Label == ConsoleGroups.Sega);
        vm.SelectedTile = vm.Tiles.First(t => t.Console == SourceConsole.Genesis);

        vm.StartOver();

        Assert.IsFalse(vm.IsGroupOpen);
        Assert.IsNull(vm.SelectedTile);
        Assert.IsNull(vm.SelectedTopTile);
        Assert.AreEqual(Assets.ConsoleGroups.Top.Count, vm.TopTiles.Count);
    }

    private InjectViewModel Create() =>
        new(new InjectBaseService(), new FakeRetroArchCores(), _dialogs, new RecordingInjectionServiceFactory(), _settings, new NavigationService(), new FakeSdCard(), new ArtworkBuilderViewModel(new FakeArtworkComposer(), _dialogs, () => _settings.WorkPath), new FakeSoundPlayer(), new FakeInjectionHistory(), new FakeCompatibilityLists(), new FakeCommunityArtwork());

    private static InjectionRecord RecordFor(SourceConsole console) =>
        new("id", DateTimeOffset.Now, console, TemplateKey.Core("genesis_plus_gx"), @"C:\game.gg", "Game", new TitleIdentity(new TitleId(TitleType.Demo, 0x31323334), new GroupId(0x3456), new ProductCode(ProductCode.EShop, "WXYZ")));
}
