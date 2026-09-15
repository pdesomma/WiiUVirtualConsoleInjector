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
    }

    [TestMethod]
    public void Refresh_Console_FillsEachSlotsFramesAndSeedsTheText()
    {
        var vm = Create();

        vm.Refresh(SourceConsole.Snes, "The Legend of Zelda, A Link to the Past", "Zelda");

        CollectionAssert.AreEqual(ArtworkFrames.For(ImageSlot.BootTv, SourceConsole.Snes).ToArray(), vm.TvFrames);
        CollectionAssert.AreEqual(ArtworkFrames.For(ImageSlot.BootDrc, SourceConsole.Snes).ToArray(), vm.GamePadFrames);
        CollectionAssert.AreEqual(ArtworkFrames.For(ImageSlot.Icon, SourceConsole.Snes).ToArray(), vm.IconFrames);
        CollectionAssert.AreEqual(ArtworkFrames.For(ImageSlot.BootLogo, SourceConsole.Snes).ToArray(), vm.LogoFrames);
        Assert.AreEqual("snes-pal", vm.TvFrame!.Key);
        Assert.AreEqual("snes-pal", vm.GamePadFrame!.Key);
        Assert.AreEqual("icon-snes-1", vm.IconFrame!.Key);
        Assert.AreEqual("logo-pill", vm.LogoFrame!.Key);
        Assert.AreEqual("The Legend of Zelda", vm.NameLine1);
        Assert.AreEqual("A Link to the Past", vm.NameLine2);
        Assert.AreEqual("Zelda", vm.LogoText);
    }

    [TestMethod]
    public void Refresh_NoShortName_LogoTakesTheFirstNameLine()
    {
        var vm = Create();

        vm.Refresh(SourceConsole.Nes, "Metroid");

        Assert.AreEqual("Metroid", vm.LogoText);
    }

    [TestMethod]
    public void Refresh_SameConsoleAgain_KeepsEachFrameChoice()
    {
        var vm = Create();
        vm.Refresh(SourceConsole.Snes, "Game");
        vm.TvFrame = vm.TvFrames.Single(f => f.Key == "snes-sfc");
        vm.GamePadFrame = vm.GamePadFrames.Single(f => f.Key == "boot-plain");
        vm.IconFrame = vm.IconFrames.Single(f => f.Key == "icon-vc");

        vm.Refresh(SourceConsole.Snes, "Renamed");

        Assert.AreEqual("snes-sfc", vm.TvFrame!.Key);
        Assert.AreEqual("boot-plain", vm.GamePadFrame!.Key);
        Assert.AreEqual("icon-vc", vm.IconFrame!.Key);
    }

    [TestMethod]
    public void Refresh_OtherConsole_DropsFramesThatNoLongerApply()
    {
        var vm = Create();
        vm.Refresh(SourceConsole.Snes, "Game");
        vm.TvFrame = vm.TvFrames.Single(f => f.Key == "snes-sfc");

        vm.Refresh(SourceConsole.Nes, "Game");

        Assert.AreEqual("nes", vm.TvFrame!.Key);
        Assert.IsFalse(vm.TvFrames.Any(f => f.Key == "snes-sfc"));
    }

    [TestMethod]
    public void CanBuild_ScreenshotSlotsNeedAScreenshotTheLogoDoesNot()
    {
        var vm = Create();
        vm.Refresh(SourceConsole.Nes, "Game");

        Assert.IsFalse(vm.CanBuild(ImageSlot.Icon));
        Assert.IsFalse(vm.CanBuild(ImageSlot.BootTv));
        Assert.IsFalse(vm.CanBuild(ImageSlot.BootDrc));
        Assert.IsTrue(vm.CanBuild(ImageSlot.BootLogo));
        Assert.IsFalse(vm.CanBuild(null));

        vm.ScreenshotPath = @"C:\shot.png";

        Assert.IsTrue(vm.CanBuild(ImageSlot.Icon));
        Assert.IsTrue(vm.BuildCommand.CanExecute(ImageSlot.BootTv));
    }

    [TestMethod]
    public async Task BuildCommand_OneSlot_DrawsOnlyThatSlotWithItsOwnFrame()
    {
        var vm = Create();
        vm.Refresh(SourceConsole.Nes, "Game", "Short");
        vm.ScreenshotPath = @"C:\shot.png";
        vm.ReleaseYear = "1985";
        vm.Players = "2";
        vm.GamePadFrame = vm.GamePadFrames.Single(f => f.Key == "boot-plain");
        var built = new List<ArtworkBuiltEventArgs>();
        vm.Built += (_, e) => built.Add(e);

        await vm.BuildCommand.ExecuteAsync(ImageSlot.BootDrc);

        var only = _composer.Composed.Single();
        Assert.AreEqual(ImageSlot.BootDrc, only.Slot);
        Assert.AreEqual("boot-plain", only.Request.Frame!.Key, "the GamePad's own frame, not the TV's");
        Assert.AreEqual(@"C:\shot.png", only.Request.ScreenshotPath);
        Assert.AreEqual("Game", only.Request.NameLine1);
        Assert.AreEqual(1985, only.Request.ReleaseYear);
        Assert.AreEqual(2, only.Request.Players);
        Assert.AreEqual(ImageSlot.BootDrc, built.Single().Slot);
        StringAssert.EndsWith(built.Single().Path, "bootDrcTex.png");
        StringAssert.StartsWith(built.Single().Path, Path.Combine(_work, "artwork"));
        Assert.IsFalse(vm.IsBuilding);
    }

    [TestMethod]
    public async Task BuildCommand_Logo_NeedsNoScreenshotAndUsesTheLogoText()
    {
        var vm = Create();
        vm.Refresh(SourceConsole.Nes, "Game", "Short");

        await vm.BuildCommand.ExecuteAsync(ImageSlot.BootLogo);

        var only = _composer.Composed.Single();
        Assert.AreEqual(ImageSlot.BootLogo, only.Slot);
        Assert.AreEqual("logo-pill", only.Request.Frame!.Key);
        Assert.AreEqual("Short", only.Request.LogoText);
        Assert.IsNull(only.Request.ScreenshotPath);
    }

    [TestMethod]
    public async Task BuildCommand_TwiceForTheSameSlot_WritesToDifferentFiles()
    {
        var vm = Create();
        vm.Refresh(SourceConsole.Nes, "Game");
        vm.ScreenshotPath = @"C:\shot.png";
        var paths = new List<string>();
        vm.Built += (_, e) => paths.Add(e.Path);

        await vm.BuildCommand.ExecuteAsync(ImageSlot.Icon);
        await vm.BuildCommand.ExecuteAsync(ImageSlot.Icon);

        Assert.AreNotEqual(paths[0], paths[1], "a fresh file each time so the slot picks up the change");
    }

    [TestMethod]
    public async Task BuildCommand_ComposerFails_ShowsTheErrorAndRaisesNothing()
    {
        var vm = Create();
        vm.Refresh(SourceConsole.Nes, "Game");
        vm.ScreenshotPath = @"C:\shot.png";
        _composer.Failure = new IOException("disk full");
        var raised = false;
        vm.Built += (_, _) => raised = true;

        await vm.BuildCommand.ExecuteAsync(ImageSlot.Icon);

        Assert.IsFalse(raised);
        StringAssert.Contains(_dialogs.Errors.Single().Message, "disk full");
        Assert.IsFalse(vm.IsBuilding);
    }

    [TestMethod]
    public async Task PickScreenshotCommand_Picked_SetsThePath()
    {
        var vm = Create();
        _dialogs.FileToPick = @"D:\shots\game.png";

        await vm.PickScreenshotCommand.ExecuteAsync(null);

        Assert.AreEqual(@"D:\shots\game.png", vm.ScreenshotPath);
        Assert.IsTrue(vm.HasScreenshot);
    }

    private ArtworkBuilderViewModel Create() => new(_composer, _dialogs, () => _work);
}
