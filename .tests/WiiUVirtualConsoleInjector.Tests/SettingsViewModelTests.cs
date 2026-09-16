using PD.WiiU.VirtualConsole;
using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class SettingsViewModelTests
{
    private static readonly AppPaths Paths = new(Path.Combine(Path.GetTempPath(), "WiiUVirtualConsoleInjector.Tests", "data"));

    private FakeDialogService _dialogs = null!;
    private FakeLinkOpener _links = null!;
    private FakeNintendontSource _nintendont = null!;
    private FakeSdCard _sdCard = null!;
    private FakeSettingsService _settings = null!;
    private UpdateNoticeViewModel _update = null!;
    private LegacyImportViewModel _legacy = null!;

    [TestInitialize]
    public void Initialize()
    {
        _dialogs = new FakeDialogService();
        _settings = new FakeSettingsService();
        _links = new FakeLinkOpener();
        _sdCard = new FakeSdCard();
        _nintendont = new FakeNintendontSource();
        _update = new UpdateNoticeViewModel(new FakeUpdateCheck(), _settings, _links, new Version(1, 0));
        _legacy = new LegacyImportViewModel(new FakeLegacyImport(), _settings, _dialogs, new FakeToastService());
    }

    [TestMethod]
    public void Constructor_Defaults_ShowsEffectivePathsAndWarnings()
    {
        var vm = Create();

        Assert.AreEqual("Settings", vm.Title);
        Assert.AreEqual(Paths.DataFolder, vm.DataFolder);
        CollectionAssert.AreEqual(new[] { vm.BaseFolder, vm.OutputFolder, vm.WorkFolder }, vm.Folders.ToArray());
        Assert.AreEqual(FakeSettingsService.DefaultBase, vm.BaseFolder.Folder);
        Assert.AreEqual(FakeSettingsService.DefaultOutput, vm.OutputFolder.Folder);
        Assert.AreEqual(FakeSettingsService.DefaultWork, vm.WorkFolder.Folder);
        CollectionAssert.AreEqual(Enum.GetValues<InjectionWarning>(), vm.Warnings.Select(w => w.Warning).ToArray());
        Assert.IsTrue(vm.Warnings.All(w => w.ShowAgain));
        Assert.IsTrue(vm.Warnings.All(w => w.Description.Length > 0));
    }

    [TestMethod]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new SettingsViewModel(null!, _dialogs, Paths, _links, _sdCard, _nintendont, _update, _legacy));
        Assert.ThrowsExactly<ArgumentNullException>(() => new SettingsViewModel(_settings, null!, Paths, _links, _sdCard, _nintendont, _update, _legacy));
        Assert.ThrowsExactly<ArgumentNullException>(() => new SettingsViewModel(_settings, _dialogs, null!, _links, _sdCard, _nintendont, _update, _legacy));
        Assert.ThrowsExactly<ArgumentNullException>(() => new SettingsViewModel(_settings, _dialogs, Paths, null!, _sdCard, _nintendont, _update, _legacy));
        Assert.ThrowsExactly<ArgumentNullException>(() => new SettingsViewModel(_settings, _dialogs, Paths, _links, null!, _nintendont, _update, _legacy));
        Assert.ThrowsExactly<ArgumentNullException>(() => new SettingsViewModel(_settings, _dialogs, Paths, _links, _sdCard, null!, _update, _legacy));
        Assert.ThrowsExactly<ArgumentNullException>(() => new SettingsViewModel(_settings, _dialogs, Paths, _links, _sdCard, _nintendont, null!, _legacy));
        Assert.ThrowsExactly<ArgumentNullException>(() => new SettingsViewModel(_settings, _dialogs, Paths, _links, _sdCard, _nintendont, _update, null!));
    }

    [TestMethod]
    public async Task Browse_FolderPicked_SavesAndShowsIt()
    {
        var vm = Create();
        var changed = new List<string>();
        vm.OutputFolder.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);
        _dialogs.FolderToPick = @"D:\out";

        await vm.OutputFolder.BrowseCommand.ExecuteAsync(null);

        Assert.AreEqual(@"D:\out", _settings.Current.OutputPath);
        Assert.AreEqual(@"D:\out", vm.OutputFolder.Folder);
        Assert.AreEqual(1, _settings.Saves);
        CollectionAssert.Contains(changed, nameof(FolderSettingViewModel.Folder));
        Assert.IsNull(_settings.Current.BasePath);
    }

    [TestMethod]
    public async Task Browse_Cancelled_LeavesSetting()
    {
        var vm = Create();

        await vm.BaseFolder.BrowseCommand.ExecuteAsync(null);

        Assert.IsNull(_settings.Current.BasePath);
        Assert.AreEqual(0, _settings.Saves);
    }

    [TestMethod]
    public async Task Browse_ExistingFolder_OpensPickerThere()
    {
        _settings.Current = new AppSettings { WorkPath = Path.GetTempPath() };
        var vm = Create();

        await vm.WorkFolder.BrowseCommand.ExecuteAsync(null);

        Assert.AreEqual(Path.GetTempPath(), _dialogs.PickedFolderStarts.Single());
    }

    [TestMethod]
    public void Reset_SetFolder_ReturnsToDefault()
    {
        _settings.Current = new AppSettings { BasePath = @"D:\bases" };
        var vm = Create();
        Assert.AreEqual(@"D:\bases", vm.BaseFolder.Folder);

        vm.BaseFolder.ResetCommand.Execute(null);

        Assert.IsNull(_settings.Current.BasePath);
        Assert.AreEqual(FakeSettingsService.DefaultBase, vm.BaseFolder.Folder);
        Assert.AreEqual(1, _settings.Saves);
    }

    [TestMethod]
    public void Warning_Unchecked_Suppresses()
    {
        var vm = Create();
        var warning = vm.Warnings.Single(w => w.Warning == InjectionWarning.SnesCoProcessor);

        warning.ShowAgain = false;

        Assert.IsTrue(_settings.Current.IsSuppressed(InjectionWarning.SnesCoProcessor));
        Assert.IsFalse(_settings.Current.IsSuppressed(InjectionWarning.NdsDsiEnhanced));
        Assert.IsFalse(warning.ShowAgain);
        Assert.AreEqual(1, _settings.Saves);
    }

    [TestMethod]
    public void Warning_Checked_Restores()
    {
        _settings.Current = new AppSettings().Suppress(InjectionWarning.GameCubeGcz);
        var vm = Create();
        var warning = vm.Warnings.Single(w => w.Warning == InjectionWarning.GameCubeGcz);
        Assert.IsFalse(warning.ShowAgain);

        warning.ShowAgain = true;

        Assert.IsFalse(_settings.Current.IsSuppressed(InjectionWarning.GameCubeGcz));
        Assert.IsTrue(warning.ShowAgain);
        Assert.AreEqual(1, _settings.Saves);
    }

    [TestMethod]
    public void Warning_SameValue_DoesNotSave()
    {
        var vm = Create();

        vm.Warnings[0].ShowAgain = true;

        Assert.AreEqual(0, _settings.Saves);
    }

    [TestMethod]
    public async Task ActivateAsync_ChangedElsewhere_RaisesPropertyChanged()
    {
        var vm = Create();
        var changed = new List<string>();
        vm.BaseFolder.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);
        vm.Warnings[0].PropertyChanged += (_, e) => changed.Add(e.PropertyName!);
        _settings.Update(s => s.Suppress(vm.Warnings[0].Warning) with { BasePath = @"E:\b" });

        await vm.ActivateAsync();

        Assert.AreEqual(@"E:\b", vm.BaseFolder.Folder);
        Assert.IsFalse(vm.Warnings[0].ShowAgain);
        CollectionAssert.Contains(changed, nameof(FolderSettingViewModel.Folder));
        CollectionAssert.Contains(changed, nameof(WarningSettingViewModel.ShowAgain));
    }

    [TestMethod]
    public async Task OpenCommand_EveryFolder_ShowsItsEffectivePathInTheFileManager()
    {
        var vm = Create();

        foreach (var folder in vm.Folders)
            await folder.OpenCommand.ExecuteAsync(null);

        CollectionAssert.AreEqual(vm.Folders.Select(f => f.Folder).ToArray(), _links.Folders);
    }

    private SettingsViewModel Create() => new(_settings, _dialogs, Paths, _links, _sdCard, _nintendont, _update, _legacy);
}
