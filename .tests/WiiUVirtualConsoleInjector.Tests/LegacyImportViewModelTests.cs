using PD.WiiU.VirtualConsole;
using WiiUSharp;
using WiiUSharp.Nus;
using WiiUVirtualConsoleInjector.Services;
using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class LegacyImportViewModelTests
{
    private static readonly BaseTitle Nes = new(TitleId.Parse("0005000010153100"), "Dr. Mario", Region.UnitedStates, SourceConsole.Nes);

    private FakeDialogService _dialogs = null!;
    private FakeLegacyImport _import = null!;
    private FakeSettingsService _settings = null!;
    private FakeToastService _toasts = null!;

    [TestInitialize]
    public void Initialize()
    {
        _dialogs = new FakeDialogService();
        _import = new FakeLegacyImport();
        _settings = new FakeSettingsService();
        _toasts = new FakeToastService();
    }

    [TestMethod]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new LegacyImportViewModel(null!, _settings, _dialogs, _toasts));
        Assert.ThrowsExactly<ArgumentNullException>(() => new LegacyImportViewModel(_import, null!, _dialogs, _toasts));
        Assert.ThrowsExactly<ArgumentNullException>(() => new LegacyImportViewModel(_import, _settings, null!, _toasts));
        Assert.ThrowsExactly<ArgumentNullException>(() => new LegacyImportViewModel(_import, _settings, _dialogs, null!));
    }

    [TestMethod]
    public void Refresh_NothingFound_HidesTheCard()
    {
        var vm = Create();

        vm.Refresh();

        Assert.IsFalse(vm.HasInstall);
        Assert.AreEqual("", vm.Summary);
        Assert.IsFalse(vm.ImportCommand.CanExecute(null));
    }

    [TestMethod]
    public void Refresh_InstallFound_SummarisesIt()
    {
        _import.Found = Install();
        var vm = Create();

        vm.Refresh();

        Assert.IsTrue(vm.HasInstall);
        Assert.AreEqual("Found the Wii U common key, 2 title keys, 1 downloaded base, its output folder, which warnings were turned off.", vm.Summary);
        Assert.AreEqual(@"C:\legacy\settings.json", vm.Location);
        Assert.IsTrue(vm.ImportCommand.CanExecute(null));
    }

    [TestMethod]
    public void OfferAtStartup_FirstTimeOnly_Toasts()
    {
        _import.Found = Install();
        var vm = Create();

        vm.OfferAtStartup();
        vm.OfferAtStartup();

        Assert.AreEqual(1, _toasts.Shown.Count);
        Assert.AreEqual(ToastKind.Info, _toasts.Shown[0].Kind);
        StringAssert.Contains(_toasts.Shown[0].Title, "UWUVCI");
        Assert.IsTrue(_settings.Current.LegacyImportOffered);
    }

    [TestMethod]
    public void OfferAtStartup_NothingFound_StaysQuiet()
    {
        Create().OfferAtStartup();

        Assert.AreEqual(0, _toasts.Shown.Count);
        Assert.IsFalse(_settings.Current.LegacyImportOffered);
    }

    [TestMethod]
    public async Task Import_Succeeds_ReportsTakesSettingsAndRaisesImported()
    {
        _import.Found = Install();
        _import.Report = new LegacyImportReport(true, 2, 1, Array.Empty<string>());
        var vm = Create();
        vm.Refresh();
        var raised = 0;
        vm.Imported += (_, _) => raised++;

        await vm.ImportCommand.ExecuteAsync(null);

        Assert.AreEqual(1, _import.Imported.Count);
        Assert.AreEqual("Took the Wii U common key, 2 title keys, 1 base, output folder.", vm.Status);
        Assert.AreEqual(@"D:\old-out", _settings.Current.OutputPath);
        Assert.IsTrue(_settings.Current.IsSuppressed(InjectionWarning.SnesCoProcessor));
        Assert.IsTrue(_settings.Current.LegacyImportOffered);
        Assert.AreEqual(1, raised);
        Assert.IsFalse(vm.IsRunning);
        Assert.IsNull(vm.Progress);
    }

    [TestMethod]
    public async Task Import_OutputFolderAlreadyChosen_KeepsOurs()
    {
        _settings.Current = new AppSettings { OutputPath = @"D:\mine" };
        _import.Found = Install();
        var vm = Create();
        vm.Refresh();

        await vm.ImportCommand.ExecuteAsync(null);

        Assert.AreEqual(@"D:\mine", _settings.Current.OutputPath);
        Assert.AreEqual("Everything was already here.", vm.Status);
    }

    [TestMethod]
    public async Task Import_PartlyFails_SaysWhich()
    {
        _import.Found = Install();
        _import.Report = new LegacyImportReport(false, 0, 0, new[] { "Dr. Mario: disk full" });
        var vm = Create();
        vm.Refresh();

        await vm.ImportCommand.ExecuteAsync(null);

        StringAssert.Contains(vm.Status, "Could not copy: Dr. Mario: disk full");
    }

    [TestMethod]
    public async Task Import_Throws_ShowsError()
    {
        _import.Found = Install();
        _import.Failure = new IOException("gone");
        var vm = Create();
        vm.Refresh();

        await vm.ImportCommand.ExecuteAsync(null);

        Assert.AreEqual(1, _dialogs.Errors.Count);
        Assert.IsFalse(vm.IsRunning);
    }

    private LegacyImportViewModel Create() => new(_import, _settings, _dialogs, _toasts);

    private static LegacyInstall Install() => new(@"C:\legacy\settings.json")
    {
        CommonKey = new CommonKey(new byte[16]),
        TitleKeys = new[] { new LegacyTitleKey(Nes.TitleId, new EncryptedTitleKey(new byte[16])), new LegacyTitleKey(TitleId.Parse("0005000010153200"), new EncryptedTitleKey(new byte[16])) },
        Bases = new[] { new LegacyBase(Nes, @"C:\legacy\bin\BaseGames\Dr. Mario [US]") },
        OutputFolder = @"D:\old-out",
        SuppressedWarnings = new[] { InjectionWarning.SnesCoProcessor },
    };
}
