using PD.WiiU.VirtualConsole;
using WiiUSharp;
using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class ArtworkBuilderViewModelTests
{
    private FakeArtworkComposer _composer = null!;
    private FakeDialogService _dialogs = null!;
    private FakeUiScheduler _scheduler = null!;
    private string _work = null!;

    [TestInitialize]
    public void Setup()
    {
        SynchronizationContext.SetSynchronizationContext(new InjectFakes.InlineSynchronizationContext());
        _composer = new FakeArtworkComposer();
        _dialogs = new FakeDialogService();
        _scheduler = new FakeUiScheduler();
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
        Assert.ThrowsExactly<ArgumentNullException>(() => new ArtworkBuilderViewModel(null!, _dialogs, _scheduler, () => _work));
        Assert.ThrowsExactly<ArgumentNullException>(() => new ArtworkBuilderViewModel(_composer, null!, _scheduler, () => _work));
        Assert.ThrowsExactly<ArgumentNullException>(() => new ArtworkBuilderViewModel(_composer, _dialogs, null!, () => _work));
        Assert.ThrowsExactly<ArgumentNullException>(() => new ArtworkBuilderViewModel(_composer, _dialogs, _scheduler, null!));
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
    public void CanApply_NeedsAScreenshotAndEveryFrame()
    {
        var vm = Create();
        Assert.IsFalse(vm.CanApply, "no frames yet");

        vm.Refresh(SourceConsole.Nes, "Game");
        Assert.IsFalse(vm.CanApply, "no screenshot yet");

        vm.ScreenshotPath = @"C:\shot.png";
        Assert.IsTrue(vm.CanApply);
        Assert.IsTrue(vm.ApplyCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task Inputs_Changed_RedrawAllFourPreviewsOnceTheyHaveSettled()
    {
        var vm = Create();
        vm.Refresh(SourceConsole.Nes, "Game", "Short");
        vm.ScreenshotPath = @"C:\shot.png";
        vm.ReleaseYear = "1985";
        vm.Players = "2";
        Assert.AreEqual(0, _composer.Composed.Count, "nothing until the inputs settle");

        _scheduler.RunDue(ArtworkBuilderViewModel.PreviewDelay);
        await vm.PreviewRender;

        Assert.AreEqual(0, _scheduler.Pending.Count, "one render for the whole burst");
        CollectionAssert.AreEquivalent(new[] { ImageSlot.BootTv, ImageSlot.BootDrc, ImageSlot.Icon, ImageSlot.BootLogo }, _composer.Composed.Select(c => c.Slot).ToArray());
        var tv = _composer.Composed.Single(c => c.Slot == ImageSlot.BootTv).Request;
        Assert.AreEqual("nes", tv.Frame!.Key);
        Assert.AreEqual(@"C:\shot.png", tv.ScreenshotPath);
        Assert.AreEqual("Game", tv.NameLine1);
        Assert.AreEqual(1985, tv.ReleaseYear);
        Assert.AreEqual(2, tv.Players);
        Assert.AreEqual("icon-nes-1", _composer.Composed.Single(c => c.Slot == ImageSlot.Icon).Request.Frame!.Key);
        Assert.AreEqual("Short", _composer.Composed.Single(c => c.Slot == ImageSlot.BootLogo).Request.LogoText);
        Assert.IsNotNull(vm.TvPreviewPath);
        Assert.IsNotNull(vm.GamePadPreviewPath);
        Assert.IsNotNull(vm.IconPreviewPath);
        Assert.IsNotNull(vm.LogoPreviewPath);
        StringAssert.StartsWith(vm.TvPreviewPath!, Path.Combine(_work, "artwork", "preview"));
        Assert.IsFalse(vm.IsRendering);
    }

    [TestMethod]
    public async Task Inputs_BlankYearAndPlayers_LeaveThoseLinesOff()
    {
        var vm = Create();
        vm.Refresh(SourceConsole.Nes, "Game");
        vm.ReleaseYear = "abc";
        vm.Players = "";

        _scheduler.RunDue(ArtworkBuilderViewModel.PreviewDelay);
        await vm.PreviewRender;

        var tv = _composer.Composed.Single(c => c.Slot == ImageSlot.BootTv).Request;
        Assert.IsNull(tv.ReleaseYear);
        Assert.IsNull(tv.Players);
    }

    [TestMethod]
    public async Task ApplyCommand_Ready_DrawsAllFourAndRaisesApplied()
    {
        var vm = Create();
        vm.Refresh(SourceConsole.Nes, "Game");
        vm.ScreenshotPath = @"C:\shot.png";
        vm.GamePadFrame = vm.GamePadFrames.Single(f => f.Key == "boot-plain");
        ArtworkBuiltEventArgs? built = null;
        vm.Applied += (_, e) => built = e;

        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.IsNotNull(built);
        StringAssert.EndsWith(built!.IconPath, "iconTex.png");
        StringAssert.EndsWith(built.BootTvPath, "bootTvTex.png");
        StringAssert.EndsWith(built.BootDrcPath, "bootDrcTex.png");
        StringAssert.EndsWith(built.BootLogoPath, "bootLogoTex.png");
        Assert.AreEqual(Path.GetDirectoryName(built.IconPath), Path.GetDirectoryName(built.BootLogoPath), "one folder per build");
        Assert.AreEqual("nes", _composer.Composed.Single(c => c.Slot == ImageSlot.BootTv).Request.Frame!.Key);
        Assert.AreEqual("boot-plain", _composer.Composed.Single(c => c.Slot == ImageSlot.BootDrc).Request.Frame!.Key, "the GamePad keeps its own frame");
        Assert.AreEqual(0, _dialogs.Errors.Count);
    }

    [TestMethod]
    public async Task ApplyCommand_ComposerFails_ShowsTheErrorAndRaisesNothing()
    {
        var vm = Create();
        vm.Refresh(SourceConsole.Nes, "Game");
        vm.ScreenshotPath = @"C:\shot.png";
        _composer.Failure = new IOException("disk full");
        var raised = false;
        vm.Applied += (_, _) => raised = true;

        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.IsFalse(raised);
        StringAssert.Contains(_dialogs.Errors.Single().Message, "disk full");
        Assert.IsFalse(vm.IsRendering);
    }

    [TestMethod]
    public async Task PickScreenshotCommand_Picked_SetsThePath()
    {
        var vm = Create();
        _dialogs.FileToPick = @"D:\shots\game.png";

        await vm.PickScreenshotCommand.ExecuteAsync(null);

        Assert.AreEqual(@"D:\shots\game.png", vm.ScreenshotPath);
    }

    private ArtworkBuilderViewModel Create() => new(_composer, _dialogs, _scheduler, () => _work);
}
