using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Options;
using WiiUSharp;
using WiiUVirtualConsoleInjector.Services;
using WiiUVirtualConsoleInjector.ViewModels;
using WiiUVirtualConsoleInjector.ViewModels.Options;
using static WiiUVirtualConsoleInjector.Tests.InjectFakes;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class OptionsViewModelTests
{
    private InjectDialogService _dialogs = null!;

    [TestInitialize]
    public void Setup() => _dialogs = new InjectDialogService();

    [TestMethod]
    public void NoOptions_Build_ReturnsNull()
    {
        var vm = new NoOptionsViewModel(SourceConsole.Msx);

        Assert.AreEqual(SourceConsole.Msx, vm.Console);
        Assert.IsNull(vm.Build());
    }

    [TestMethod]
    public void Nes_Build_MapsPixelPerfect()
    {
        var vm = new NesOptionsViewModel();
        Assert.IsFalse(((NesOptions)vm.Build()!).PixelPerfect);

        vm.PixelPerfect = true;

        var built = (NesOptions)vm.Build()!;
        Assert.AreEqual(SourceConsole.Nes, built.Console);
        Assert.IsTrue(built.PixelPerfect);
    }

    [TestMethod]
    public void Snes_Build_MapsPixelPerfect()
    {
        var vm = new SnesOptionsViewModel { PixelPerfect = true };

        var built = (SnesOptions)vm.Build()!;

        Assert.AreEqual(SourceConsole.Snes, built.Console);
        Assert.IsTrue(built.PixelPerfect);
    }

    [TestMethod]
    public void Gba_Build_MapsEveryProperty()
    {
        var vm = new GbaOptionsViewModel { PokemonPatch = true, RemoveDarkFilter = true };

        var built = (GbaOptions)vm.Build()!;

        Assert.IsTrue(built.PokemonPatch);
        Assert.IsTrue(built.RemoveDarkFilter);
    }

    [TestMethod]
    public void N64_Build_MapsEveryProperty()
    {
        var vm = new N64OptionsViewModel(_dialogs) { RemoveDarkFilter = true, WideScreen = true };
        Assert.IsNull(((N64Options)vm.Build()!).IniPath);
        vm.Ini.Path = @"C:\game.ini";

        var built = (N64Options)vm.Build()!;

        Assert.AreEqual(@"C:\game.ini", built.IniPath);
        Assert.IsTrue(built.RemoveDarkFilter);
        Assert.IsTrue(built.WideScreen);
        Assert.IsFalse(vm.Ini.IsFolder);
    }

    [TestMethod]
    public void Nds_Build_MapsEveryProperty()
    {
        var vm = new NdsOptionsViewModel(_dialogs);
        Assert.AreEqual(NdsOptions.DefaultBrightness, vm.Brightness);
        Assert.IsTrue(vm.LayoutScreens.IsFolder, "layout screens are a folder");
        vm.Brightness = 55;
        vm.PixelArtUpscaler = 4;
        vm.LayoutScreens.Path = @"C:\layout";

        var built = (NdsOptions)vm.Build()!;

        Assert.AreEqual(55, built.Brightness);
        Assert.AreEqual(4, built.PixelArtUpscaler);
        Assert.AreEqual(@"C:\layout", built.LayoutScreensPath);
    }

    [TestMethod]
    public void GameCube_Build_MapsEveryProperty()
    {
        var vm = new GameCubeOptionsViewModel(_dialogs) { ForceFourByThree = true, KeepFullImage = true };
        vm.Forwarder.Path = @"C:\forwarder.dol";
        vm.SecondDisc.Path = @"C:\disc2.gcm";

        var built = (GameCubeOptions)vm.Build()!;

        Assert.IsTrue(built.ForceFourByThree);
        Assert.IsTrue(built.KeepFullImage);
        Assert.IsFalse(((GameCubeOptions)new GameCubeOptionsViewModel(_dialogs).Build()!).KeepFullImage, "compaction is the default");
        Assert.AreEqual(@"C:\forwarder.dol", built.ForwarderPath);
        Assert.AreEqual(@"C:\disc2.gcm", built.SecondDiscPath);
    }

    [TestMethod]
    public void Wii_Build_DefaultsMatchDomainDefaults()
    {
        var vm = new WiiOptionsViewModel(_dialogs);

        var built = (WiiOptions)vm.Build()!;
        var defaults = new WiiOptions();

        Assert.AreEqual(defaults.ControllerMode, built.ControllerMode);
        Assert.AreEqual(defaults.Passthrough, built.Passthrough);
        Assert.AreEqual(defaults.TrimDisc, built.TrimDisc);
        Assert.AreEqual(defaults.VideoMode, built.VideoMode);
        Assert.IsNull(built.TargetRegion);
        Assert.IsNull(built.CheatCodesPath);
        Assert.IsNull(built.ForwarderPath);
        Assert.AreEqual(Enum.GetValues<WiiControllerMode>().Length, vm.ControllerModes.Count);
        Assert.AreEqual(Enum.GetValues<WiiVideoMode>().Length, vm.VideoModes.Count);
        Assert.AreEqual(4, vm.TargetRegions.Count);
    }

    [TestMethod]
    public void Wii_Build_MapsEveryProperty()
    {
        var vm = new WiiOptionsViewModel(_dialogs)
        {
            ControllerMode = WiiControllerMode.HorizontalWiiRemote,
            ForceFourByThree = true,
            HalfVerticalFilter = true,
            LrPatch = true,
            Passthrough = false,
            RemoveDeflicker = true,
            RemoveDithering = true,
            TargetRegion = Choice("Japan"),
            TrimDisc = false,
            VideoMode = WiiVideoMode.Pal60,
        };
        vm.CheatCodes.Path = @"C:\codes.gct";
        vm.Forwarder.Path = @"C:\booter.dol";

        var built = (WiiOptions)vm.Build()!;

        Assert.AreEqual(@"C:\codes.gct", built.CheatCodesPath);
        Assert.AreEqual(WiiControllerMode.HorizontalWiiRemote, built.ControllerMode);
        Assert.IsTrue(built.ForceFourByThree);
        Assert.AreEqual(@"C:\booter.dol", built.ForwarderPath);
        Assert.IsTrue(built.HalfVerticalFilter);
        Assert.IsTrue(built.LrPatch);
        Assert.IsFalse(built.Passthrough);
        Assert.IsTrue(built.RemoveDeflicker);
        Assert.IsTrue(built.RemoveDithering);
        Assert.AreEqual(Region.Japan, built.TargetRegion);
        Assert.IsFalse(built.TrimDisc);
        Assert.AreEqual(WiiVideoMode.Pal60, built.VideoMode);
    }

    [TestMethod]
    public void RegionChoice_All_MapsLabelsToRegions()
    {
        Assert.IsNull(RegionChoice.All[0].Region);
        Assert.AreEqual("Unchanged", RegionChoice.All[0].ToString());
        Assert.AreEqual(Region.Japan, Choice("Japan").Region);
        Assert.AreEqual(Region.UnitedStates, Choice("United States").Region);
        Assert.AreEqual(Region.Europe, Choice("Europe").Region);
    }

    [TestMethod]
    public async Task PathField_PickFile_UsesFiltersAndKeepsChoice()
    {
        var field = new PathFieldViewModel(_dialogs, "INI", "hint", new FileFilter("INI", "*.ini"));
        Assert.IsFalse(field.HasPath);
        Assert.IsFalse(field.ClearCommand.CanExecute(null));
        _dialogs.NextPaths.Enqueue(@"C:\a.ini");

        await field.PickCommand.ExecuteAsync(null);

        Assert.AreEqual(@"C:\a.ini", field.Path);
        Assert.IsTrue(field.HasPath);
        Assert.AreEqual("INI", _dialogs.FilePicks.Single().Title);
        CollectionAssert.AreEqual(new[] { "*.ini" }, _dialogs.FilePicks.Single().Filters.Single().Patterns);
        Assert.AreEqual("hint", field.Hint);
    }

    [TestMethod]
    public async Task PathField_PickCancelled_KeepsPreviousPath()
    {
        var field = new PathFieldViewModel(_dialogs, "INI", null, new FileFilter("INI", "*.ini")) { Path = @"C:\old.ini" };

        await field.PickCommand.ExecuteAsync(null);

        Assert.AreEqual(@"C:\old.ini", field.Path);
    }

    [TestMethod]
    public async Task PathField_NoFilters_PicksFolder()
    {
        var field = new PathFieldViewModel(_dialogs, "Layout", null);
        _dialogs.NextPaths.Enqueue(@"C:\layout");

        await field.PickCommand.ExecuteAsync(null);

        Assert.IsTrue(field.IsFolder);
        Assert.AreEqual(@"C:\layout", field.Path);
        Assert.AreEqual("Layout", _dialogs.FolderPicks.Single());
        Assert.AreEqual(0, _dialogs.FilePicks.Count);
    }

    [TestMethod]
    public void PathField_Clear_ForgetsPath()
    {
        var field = new PathFieldViewModel(_dialogs, "INI", null, new FileFilter("INI", "*.ini")) { Path = @"C:\a.ini" };
        Assert.IsTrue(field.ClearCommand.CanExecute(null));

        field.ClearCommand.Execute(null);

        Assert.IsNull(field.Path);
        Assert.IsFalse(field.ClearCommand.CanExecute(null));
    }

    [TestMethod]
    public void PathField_NullArguments_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new PathFieldViewModel(null!, "x", null));
        Assert.ThrowsExactly<ArgumentNullException>(() => new PathFieldViewModel(_dialogs, null!, null));
    }

    [TestMethod]
    public void Load_EachConsole_ShowsTheOptionsAndNullResets()
    {
        var nes = new NesOptionsViewModel();
        nes.Load(new NesOptions { PixelPerfect = true });
        Assert.IsTrue(nes.PixelPerfect);
        nes.Load(null);
        Assert.IsFalse(nes.PixelPerfect);

        var snes = new SnesOptionsViewModel();
        snes.Load(new SnesOptions { PixelPerfect = true });
        Assert.IsTrue(snes.PixelPerfect);
        snes.Load(new NesOptions { PixelPerfect = true });
        Assert.IsFalse(snes.PixelPerfect, "another console's options reset");

        var gba = new GbaOptionsViewModel();
        gba.Load(new GbaOptions { PokemonPatch = true, RemoveDarkFilter = true });
        Assert.IsTrue(gba.PokemonPatch && gba.RemoveDarkFilter);

        var n64 = new N64OptionsViewModel(_dialogs);
        n64.Load(new N64Options { IniPath = @"C:\g.ini", RemoveDarkFilter = true, WideScreen = true });
        Assert.AreEqual(@"C:\g.ini", n64.Ini.Path);
        Assert.IsTrue(n64.RemoveDarkFilter && n64.WideScreen);

        var nds = new NdsOptionsViewModel(_dialogs);
        nds.Load(new NdsOptions { Brightness = 40, LayoutScreensPath = @"C:\layout", PixelArtUpscaler = 2 });
        Assert.AreEqual(40, nds.Brightness);
        Assert.AreEqual(@"C:\layout", nds.LayoutScreens.Path);
        Assert.AreEqual(2, nds.PixelArtUpscaler);
        nds.Load(null);
        Assert.AreEqual(NdsOptions.DefaultBrightness, nds.Brightness);

        var wii = new WiiOptionsViewModel(_dialogs);
        wii.Load(new WiiOptions { CheatCodesPath = @"C:\c.gct", ControllerMode = WiiControllerMode.WiiRemote, ForceFourByThree = true, ForwarderPath = @"C:\f.dol", HalfVerticalFilter = true, LrPatch = true, Passthrough = false, RemoveDeflicker = true, RemoveDithering = true, TargetRegion = Region.Japan, TrimDisc = false, VideoMode = WiiVideoMode.Ntsc });
        Assert.AreEqual(@"C:\c.gct", wii.CheatCodes.Path);
        Assert.AreEqual(WiiControllerMode.WiiRemote, wii.ControllerMode);
        Assert.IsTrue(wii.ForceFourByThree && wii.HalfVerticalFilter && wii.LrPatch && wii.RemoveDeflicker && wii.RemoveDithering);
        Assert.IsFalse(wii.Passthrough || wii.TrimDisc);
        Assert.AreEqual(@"C:\f.dol", wii.Forwarder.Path);
        Assert.AreEqual(Choice("Japan"), wii.TargetRegion);
        Assert.AreEqual(WiiVideoMode.Ntsc, wii.VideoMode);
        wii.Load(null);
        Assert.AreEqual(Choice("Unchanged"), wii.TargetRegion);
        Assert.IsTrue(wii.Passthrough && wii.TrimDisc);

        var cube = new GameCubeOptionsViewModel(_dialogs);
        cube.Load(new GameCubeOptions { ForceFourByThree = true, KeepFullImage = true, ForwarderPath = @"C:\n.dol", SecondDiscPath = @"C:\d2.iso" });
        Assert.IsTrue(cube.ForceFourByThree);
        Assert.IsTrue(cube.KeepFullImage);
        Assert.AreEqual(@"C:\n.dol", cube.Forwarder.Path);
        Assert.AreEqual(@"C:\d2.iso", cube.SecondDisc.Path);

        new NoOptionsViewModel(SourceConsole.Msx).Load(new NesOptions());
    }

    private static RegionChoice Choice(string label) => RegionChoice.All.Single(r => r.Label == label);
}
