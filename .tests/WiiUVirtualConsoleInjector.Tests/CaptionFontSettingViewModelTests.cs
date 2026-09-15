using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class CaptionFontSettingViewModelTests
{
    private FakeDialogService _dialogs = null!;
    private FakeSettingsService _settings = null!;

    [TestInitialize]
    public void Setup()
    {
        _dialogs = new FakeDialogService();
        _settings = new FakeSettingsService();
    }

    [TestMethod]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new CaptionFontSettingViewModel(null!, _dialogs));
        Assert.ThrowsExactly<ArgumentNullException>(() => new CaptionFontSettingViewModel(_settings, null!));
    }

    [TestMethod]
    public void Status_NothingChosenOrFound_SaysBundled()
    {
        var vm = new CaptionFontSettingViewModel(_settings, _dialogs);

        Assert.AreEqual(CaptionFontSettingViewModel.BundledText, vm.Status);
        Assert.IsFalse(vm.IsChosen);
    }

    [TestMethod]
    public void Status_LegacyFontFound_ShowsItWithoutCountingAsChosen()
    {
        _settings.FoundCaptionFont = @"C:\legacy\font.otf";
        var vm = new CaptionFontSettingViewModel(_settings, _dialogs);

        Assert.AreEqual(@"C:\legacy\font.otf", vm.Status);
        Assert.IsFalse(vm.IsChosen);
    }

    [TestMethod]
    public async Task BrowseCommand_Picked_SavesAndShowsIt()
    {
        var vm = new CaptionFontSettingViewModel(_settings, _dialogs);
        _dialogs.FileToPick = @"D:\fonts\rodin.otf";
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        await vm.BrowseCommand.ExecuteAsync(null);

        Assert.AreEqual(@"D:\fonts\rodin.otf", _settings.Current.CaptionFontPath);
        Assert.AreEqual(@"D:\fonts\rodin.otf", vm.Status);
        Assert.IsTrue(vm.IsChosen);
        CollectionAssert.Contains(changed, nameof(CaptionFontSettingViewModel.Status));
    }

    [TestMethod]
    public async Task BrowseCommand_Cancelled_ChangesNothing()
    {
        var vm = new CaptionFontSettingViewModel(_settings, _dialogs);

        await vm.BrowseCommand.ExecuteAsync(null);

        Assert.AreEqual(0, _settings.Saves);
    }

    [TestMethod]
    public void ResetCommand_Chosen_ForgetsIt()
    {
        _settings.Current = _settings.Current with { CaptionFontPath = @"D:\fonts\rodin.otf" };
        var vm = new CaptionFontSettingViewModel(_settings, _dialogs);

        vm.ResetCommand.Execute(null);

        Assert.IsNull(_settings.Current.CaptionFontPath);
        Assert.IsFalse(vm.IsChosen);
        Assert.AreEqual(CaptionFontSettingViewModel.BundledText, vm.Status);
    }
}
