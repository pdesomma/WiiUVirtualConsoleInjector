using PD.WiiU.VirtualConsole.Wii;
using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class NintendontSettingViewModelTests
{
    private FakeDialogService _dialogs = null!;
    private string? _root;
    private FakeNintendontSource _source = null!;

    [TestInitialize]
    public void Initialize()
    {
        _dialogs = new FakeDialogService();
        _source = new FakeNintendontSource();
        _root = Path.Combine(Path.GetTempPath(), "NintendontSettingViewModelTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (_root is not null && Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new NintendontSettingViewModel(null!, () => _root, _dialogs));
        Assert.ThrowsExactly<ArgumentNullException>(() => new NintendontSettingViewModel(_source, null!, _dialogs));
        Assert.ThrowsExactly<ArgumentNullException>(() => new NintendontSettingViewModel(_source, () => _root, null!));
    }

    [TestMethod]
    public void Refresh_NoCard_NothingToDo()
    {
        _root = null;
        var vm = Create();

        vm.Refresh();

        Assert.IsFalse(vm.HasCard);
        Assert.IsFalse(vm.HasLoader);
        Assert.IsFalse(vm.InstallCommand.CanExecute(null));
        Assert.IsFalse(vm.WriteConfigCommand.CanExecute(null));
        StringAssert.Contains(vm.LoaderStatus, "Pick the card");
    }

    [TestMethod]
    public void Refresh_ConfigOnCard_LoadsItsSettings()
    {
        File.WriteAllBytes(Path.Combine(_root!, "nincfg.bin"), new NintendontConfig { Video = NintendontVideo.Force, ForcedMode = NintendontForcedMode.Pal60, Pads = 1, Language = NintendontLanguage.French, SkipIpl = true }.ToBytes());
        var vm = Create();

        vm.Refresh();

        Assert.IsTrue(vm.HasConfig);
        Assert.IsFalse(vm.HasLoader);
        Assert.AreEqual(NintendontVideo.Force, vm.Video);
        Assert.IsTrue(vm.IsForcing);
        Assert.AreEqual(NintendontForcedMode.Pal60, vm.ForcedMode);
        Assert.AreEqual(1, vm.Pads);
        Assert.AreEqual(NintendontLanguage.French, vm.Language);
        Assert.IsTrue(vm.SkipIpl);
        StringAssert.Contains(vm.LoaderStatus, "not on the card");
    }

    [TestMethod]
    public void Refresh_GarbageConfig_KeepsDefaults()
    {
        File.WriteAllBytes(Path.Combine(_root!, "nincfg.bin"), new byte[600]);
        var vm = Create();

        vm.Refresh();

        Assert.AreEqual(NintendontVideo.Auto, vm.Video);
        Assert.AreEqual(4, vm.Pads);
    }

    [TestMethod]
    public async Task Install_Succeeds_WritesLoaderAndDefaultConfig()
    {
        var vm = Create();
        vm.Refresh();
        vm.ForceWidescreen = true;

        await vm.InstallCommand.ExecuteAsync(null);

        CollectionAssert.AreEqual(new[] { _root }, _source.Installs);
        Assert.IsTrue(vm.HasLoader);
        Assert.IsTrue(vm.HasConfig);
        Assert.IsTrue(NintendontConfig.Parse(File.ReadAllBytes(Path.Combine(_root!, "nincfg.bin"))).ForceWidescreen);
        Assert.AreEqual("Nintendont is on the card.", vm.Progress);
        Assert.IsFalse(vm.IsBusy);
    }

    [TestMethod]
    public async Task Install_ConfigAlreadyThere_LeavesIt()
    {
        File.WriteAllBytes(Path.Combine(_root!, "nincfg.bin"), new NintendontConfig { Pads = 3 }.ToBytes());
        var vm = Create();
        vm.Refresh();
        vm.Pads = 1;

        await vm.InstallCommand.ExecuteAsync(null);

        Assert.AreEqual(3, vm.Pads, "reloaded from the untouched file");
    }

    [TestMethod]
    public async Task Install_Fails_ShowsError()
    {
        _source.Failure = new IOException("card gone");
        var vm = Create();
        vm.Refresh();

        await vm.InstallCommand.ExecuteAsync(null);

        Assert.AreEqual(1, _dialogs.Errors.Count);
        StringAssert.Contains(_dialogs.Errors[0].Message, "card gone");
        Assert.IsNull(vm.Progress);
        Assert.IsFalse(vm.HasConfig);
    }

    [TestMethod]
    public async Task WriteConfig_FieldsSet_WritesTheFile()
    {
        var vm = Create();
        vm.Refresh();
        vm.Video = NintendontVideo.ForceDeflicker;
        vm.ForcedMode = NintendontForcedMode.MPal;
        vm.MemoryCardEmulation = false;
        vm.MemoryCardSize = 4;
        vm.Cheats = true;

        await vm.WriteConfigCommand.ExecuteAsync(null);

        var config = NintendontConfig.Parse(File.ReadAllBytes(Path.Combine(_root!, "nincfg.bin")));
        Assert.AreEqual(NintendontVideo.ForceDeflicker, config.Video);
        Assert.AreEqual(NintendontForcedMode.MPal, config.ForcedMode);
        Assert.IsFalse(config.MemoryCardEmulation);
        Assert.AreEqual(4, config.MemoryCardSize);
        Assert.IsTrue(config.Cheats);
        Assert.IsTrue(config.AutoBoot);
        Assert.AreEqual("nincfg.bin written.", vm.Progress);
    }

    [TestMethod]
    public void Load_Null_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => Create().Load(null!));
    }

    [TestMethod]
    public void Build_ThenLoad_RoundTripsEveryField()
    {
        var vm = Create();
        vm.ArcadeMode = true;
        vm.BroadbandEmulation = true;
        vm.ClassicControllerRumble = true;
        vm.ForceProgressive = true;
        vm.PatchPal50 = true;
        vm.Progressive = true;
        vm.RemoveLimit = true;
        vm.WiiUWidescreen = true;
        vm.MemoryCardShared = true;
        vm.Language = NintendontLanguage.Dutch;
        var other = Create();

        other.Load(vm.Build());

        CollectionAssert.AreEqual(vm.Build().ToBytes(), other.Build().ToBytes());
        Assert.IsTrue(other.ArcadeMode && other.BroadbandEmulation && other.ClassicControllerRumble && other.ForceProgressive && other.PatchPal50 && other.Progressive && other.RemoveLimit && other.WiiUWidescreen && other.MemoryCardShared);
    }

    private NintendontSettingViewModel Create() => new(_source, () => _root, _dialogs);
}
