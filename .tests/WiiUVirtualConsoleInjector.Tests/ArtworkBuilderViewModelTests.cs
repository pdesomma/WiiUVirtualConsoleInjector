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
    public void Refresh_Console_ListsItsFramesAndSplitsTheNameOnCommas()
    {
        var vm = Create();

        vm.Refresh(SourceConsole.Snes, "The Legend of Zelda, A Link to the Past");

        CollectionAssert.AreEqual(ArtworkTemplates.For(SourceConsole.Snes).ToArray(), vm.Templates);
        Assert.AreEqual("snes-pal", vm.SelectedTemplate!.Key);
        Assert.AreEqual("The Legend of Zelda", vm.NameLine1);
        Assert.AreEqual("A Link to the Past", vm.NameLine2);
    }

    [TestMethod]
    public void Refresh_SameConsoleAgain_KeepsTheChosenFrame()
    {
        var vm = Create();
        vm.Refresh(SourceConsole.Snes, "Game");
        vm.SelectedTemplate = vm.Templates.Single(t => t.Key == "snes-sfc");

        vm.Refresh(SourceConsole.Snes, "Renamed");

        Assert.AreEqual("snes-sfc", vm.SelectedTemplate!.Key);
        Assert.AreEqual("Renamed", vm.NameLine1);
        Assert.IsNull(vm.NameLine2);
    }

    [TestMethod]
    public void CanApply_NeedsAFrameAndAScreenshot()
    {
        var vm = Create();
        Assert.IsFalse(vm.CanApply);

        vm.Refresh(SourceConsole.Nes, "Game");
        Assert.IsFalse(vm.CanApply, "no screenshot yet");

        vm.ScreenshotPath = @"C:\shot.png";
        Assert.IsTrue(vm.CanApply);
        Assert.IsTrue(vm.ApplyCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task Inputs_Changed_RedrawThePreviewOnceTheyHaveSettled()
    {
        var vm = Create();
        vm.Refresh(SourceConsole.Nes, "Game");
        vm.ScreenshotPath = @"C:\shot.png";
        vm.ReleaseYear = "1985";
        vm.Players = "2";
        Assert.AreEqual(0, _composer.Composed.Count, "nothing until the inputs settle");

        _scheduler.RunDue(ArtworkBuilderViewModel.PreviewDelay);
        await vm.PreviewRender;

        Assert.AreEqual(0, _scheduler.Pending.Count, "one render for the whole burst");
        CollectionAssert.AreEquivalent(new[] { ImageSlot.BootTv, ImageSlot.Icon }, _composer.Composed.Select(c => c.Slot).ToArray());
        var request = _composer.Composed[0].Request;
        Assert.AreEqual("nes", request.Template.Key);
        Assert.AreEqual(@"C:\shot.png", request.ScreenshotPath);
        Assert.AreEqual("Game", request.NameLine1);
        Assert.AreEqual(1985, request.ReleaseYear);
        Assert.AreEqual(2, request.Players);
        Assert.IsNotNull(vm.PreviewPath);
        Assert.IsNotNull(vm.IconPreviewPath);
        StringAssert.StartsWith(vm.PreviewPath!, Path.Combine(_work, "artwork", "preview"));
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

        Assert.IsNull(_composer.Composed[0].Request.ReleaseYear);
        Assert.IsNull(_composer.Composed[0].Request.Players);
    }

    [TestMethod]
    public async Task ApplyCommand_Ready_DrawsAllThreeAndRaisesApplied()
    {
        var vm = Create();
        vm.Refresh(SourceConsole.Nes, "Game");
        vm.ScreenshotPath = @"C:\shot.png";
        ArtworkBuiltEventArgs? built = null;
        vm.Applied += (_, e) => built = e;

        await vm.ApplyCommand.ExecuteAsync(null);

        Assert.IsNotNull(built);
        StringAssert.EndsWith(built!.IconPath, "iconTex.png");
        StringAssert.EndsWith(built.BootTvPath, "bootTvTex.png");
        StringAssert.EndsWith(built.BootDrcPath, "bootDrcTex.png");
        Assert.AreEqual(Path.GetDirectoryName(built.IconPath), Path.GetDirectoryName(built.BootDrcPath), "one folder per build");
        CollectionAssert.AreEquivalent(new[] { ImageSlot.Icon, ImageSlot.BootTv, ImageSlot.BootDrc }, _composer.Composed.Select(c => c.Slot).ToArray());
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
