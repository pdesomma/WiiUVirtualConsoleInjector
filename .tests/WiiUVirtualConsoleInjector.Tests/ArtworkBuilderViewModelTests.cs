using PD.WiiU.VirtualConsole;
using WiiUSharp;
using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class ArtworkBuilderViewModelTests
{
    private FakeArtworkComposer _composer = null!;
    private FakeDialogService _dialogs = null!;
    private string _work = null!;

    [TestInitialize]
    public void Setup()
    {
        SynchronizationContext.SetSynchronizationContext(new InjectFakes.InlineSynchronizationContext());
        _composer = new FakeArtworkComposer();
        _dialogs = new FakeDialogService();
        _work = Path.Combine(Path.GetTempPath(), "artwork-vm-" + Guid.NewGuid().ToString("N"));
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_work))
            Directory.Delete(_work, recursive: true);
    }

    [TestMethod]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new ArtworkBuilderViewModel(null!, _dialogs, () => _work));
        Assert.ThrowsExactly<ArgumentNullException>(() => new ArtworkBuilderViewModel(_composer, null!, () => _work));
        Assert.ThrowsExactly<ArgumentNullException>(() => new ArtworkBuilderViewModel(_composer, _dialogs, null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => new ArtworkSlotViewModel(null!, _composer, _dialogs, () => _work));
    }

    [TestMethod]
    public void Constructor_Always_HasOneBuilderPerSlotThatKnowsWhatItIs()
    {
        var vm = Create();

        CollectionAssert.AreEqual(new[] { ImageSlot.Icon, ImageSlot.BootTv, ImageSlot.BootDrc, ImageSlot.BootLogo }, vm.Slots.Select(s => s.Slot).ToArray());
        Assert.IsFalse(vm.Icon.IsBootScreen);
        Assert.IsTrue(vm.Tv.IsBootScreen);
        Assert.IsTrue(vm.GamePad.IsBootScreen);
        Assert.IsTrue(vm.Logo.IsLogo);
        Assert.IsFalse(vm.Tv.IsLogo);
    }

    [TestMethod]
    public void Refresh_Console_GivesEachSlotItsOwnOverlaysEndingInNone()
    {
        var vm = Create();

        vm.Refresh(SourceConsole.Snes, "The Legend of Zelda, A Link to the Past", "Zelda");

        CollectionAssert.AreEqual(ArtworkFrames.For(ImageSlot.BootTv, SourceConsole.Snes).ToArray(), vm.Tv.Overlays);
        CollectionAssert.AreEqual(ArtworkFrames.For(ImageSlot.Icon, SourceConsole.Snes).ToArray(), vm.Icon.Overlays);
        Assert.AreEqual("None", vm.Tv.Overlays[^1].Name);
        Assert.AreEqual("None", vm.Icon.Overlays[^1].Name);
        Assert.AreEqual("None", vm.Logo.Overlays[^1].Name);
        Assert.AreEqual("snes-pal", vm.Tv.Overlay!.Key);
        Assert.AreEqual("icon-snes-1", vm.Icon.Overlay!.Key);
        Assert.AreEqual("logo-pill", vm.Logo.Overlay!.Key);
    }

    [TestMethod]
    public void Refresh_Names_SeedBlankCaptionsButNeverOverwriteTyping()
    {
        var vm = Create();
        vm.Refresh(SourceConsole.Snes, "The Legend of Zelda, A Link to the Past", "Zelda");
        Assert.AreEqual("The Legend of Zelda", vm.Tv.NameLine1);
        Assert.AreEqual("A Link to the Past", vm.GamePad.NameLine2);
        Assert.AreEqual("Zelda", vm.Logo.LogoText);

        vm.Tv.NameLine1 = "Mine";
        vm.Refresh(SourceConsole.Snes, "Renamed", "Short");

        Assert.AreEqual("Mine", vm.Tv.NameLine1, "typed text stays");
        Assert.AreEqual("Zelda", vm.Logo.LogoText, "already seeded text stays");
    }

    [TestMethod]
    public void Refresh_SameConsoleAgain_KeepsEachSlotsOverlayChoice()
    {
        var vm = Create();
        vm.Refresh(SourceConsole.Snes, "Game");
        vm.Tv.Overlay = vm.Tv.Overlays.Single(f => f.Key == "snes-sfc");
        vm.GamePad.Overlay = vm.GamePad.Overlays.Single(f => f.Key == "boot-plain");

        vm.Refresh(SourceConsole.Snes, "Renamed");

        Assert.AreEqual("snes-sfc", vm.Tv.Overlay!.Key);
        Assert.AreEqual("boot-plain", vm.GamePad.Overlay!.Key);
    }

    [TestMethod]
    public void CanBuild_ImageSlotsNeedTheirOwnSourceTheLogoDoesNot()
    {
        var vm = Create();
        vm.Refresh(SourceConsole.Nes, "Game");

        Assert.IsFalse(vm.Icon.CanBuild);
        Assert.IsFalse(vm.Tv.CanBuild);
        Assert.IsTrue(vm.Logo.CanBuild);

        vm.Tv.SourcePath = @"C:\tv.png";

        Assert.IsTrue(vm.Tv.CanBuild);
        Assert.IsFalse(vm.Icon.CanBuild, "the icon has its own source");
        Assert.IsFalse(vm.GamePad.CanBuild, "so does the GamePad");
    }

    [TestMethod]
    public async Task Build_OneSlot_UsesOnlyThatSlotsInputsAndTouchesNothingElse()
    {
        var vm = Create();
        vm.Refresh(SourceConsole.Nes, "Game", "Short");
        vm.Tv.SourcePath = @"C:\tv.png";
        vm.GamePad.SourcePath = @"C:\pad.png";
        vm.GamePad.Overlay = vm.GamePad.Overlays.Single(f => f.Key == "boot-plain");
        vm.GamePad.NameLine1 = "Pad name";
        vm.GamePad.ReleaseYear = "1985";
        vm.GamePad.Players = "2";
        var built = new List<ArtworkBuiltEventArgs>();
        vm.Built += (_, e) => built.Add(e);

        await vm.GamePad.BuildCommand.ExecuteAsync(null);

        var only = _composer.Composed.Single();
        Assert.AreEqual(ImageSlot.BootDrc, only.Slot);
        Assert.AreEqual(@"C:\pad.png", only.Request.ScreenshotPath, "the GamePad's own image");
        Assert.AreEqual("boot-plain", only.Request.Frame!.Key, "the GamePad's own overlay");
        Assert.AreEqual("Pad name", only.Request.NameLine1);
        Assert.AreEqual(1985, only.Request.ReleaseYear);
        Assert.AreEqual(2, only.Request.Players);
        Assert.AreEqual(ImageSlot.BootDrc, built.Single().Slot);
        StringAssert.EndsWith(built.Single().Path, "bootDrcTex.png");
        Assert.IsFalse(vm.GamePad.IsBuilding);
    }

    [TestMethod]
    public async Task Build_Logo_UsesItsTextAndNoImage()
    {
        var vm = Create();
        vm.Refresh(SourceConsole.Nes, "Game", "Short");
        vm.Logo.LogoText = "Logo words";

        await vm.Logo.BuildCommand.ExecuteAsync(null);

        var only = _composer.Composed.Single();
        Assert.AreEqual(ImageSlot.BootLogo, only.Slot);
        Assert.AreEqual("logo-pill", only.Request.Frame!.Key);
        Assert.AreEqual("Logo words", only.Request.LogoText);
        Assert.IsNull(only.Request.ScreenshotPath);
    }

    [TestMethod]
    public async Task Build_TwiceForTheSameSlot_WritesToDifferentFiles()
    {
        var vm = Create();
        vm.Refresh(SourceConsole.Nes, "Game");
        vm.Icon.SourcePath = @"C:\icon.png";
        var paths = new List<string>();
        vm.Built += (_, e) => paths.Add(e.Path);

        await vm.Icon.BuildCommand.ExecuteAsync(null);
        await vm.Icon.BuildCommand.ExecuteAsync(null);

        Assert.AreNotEqual(paths[0], paths[1], "a fresh file each time so the slot picks up the change");
    }

    [TestMethod]
    public async Task Build_ComposerFails_ShowsTheErrorAndRaisesNothing()
    {
        var vm = Create();
        vm.Refresh(SourceConsole.Nes, "Game");
        vm.Icon.SourcePath = @"C:\icon.png";
        _composer.Failure = new IOException("disk full");
        var raised = false;
        vm.Built += (_, _) => raised = true;

        await vm.Icon.BuildCommand.ExecuteAsync(null);

        Assert.IsFalse(raised);
        StringAssert.Contains(_dialogs.Errors.Single().Message, "disk full");
        Assert.IsFalse(vm.Icon.IsBuilding);
    }

    [TestMethod]
    public async Task PickSourceCommand_Picked_SetsOnlyThatSlotsSource()
    {
        var vm = Create();
        _dialogs.FileToPick = @"D:\shots\game.png";

        await vm.Tv.PickSourceCommand.ExecuteAsync(null);

        Assert.AreEqual(@"D:\shots\game.png", vm.Tv.SourcePath);
        Assert.IsTrue(vm.Tv.HasSource);
        Assert.IsNull(vm.GamePad.SourcePath);
        Assert.IsNull(vm.Icon.SourcePath);
    }

    private ArtworkBuilderViewModel Create() => new(_composer, _dialogs, () => _work);
}
