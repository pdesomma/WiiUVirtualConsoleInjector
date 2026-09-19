using PD.WiiU.VirtualConsole;
using WiiUSharp;
using WiiUVirtualConsoleInjector.Services;
using WiiUVirtualConsoleInjector.ViewModels;
using static WiiUVirtualConsoleInjector.Tests.InjectFakes;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class InjectRetroArchTests
{
    private const string GenesisRom = @"C:\sonic.md";

    private InjectBaseService _bases = null!;
    private FakeRetroArchCores _cores = null!;
    private InjectDialogService _dialogs = null!;
    private RecordingInjectionServiceFactory _factory = null!;
    private FakeInjectionHistory _history = null!;
    private readonly NavigationService _navigation = new();
    private RetroArchCore _picodrive = null!;
    private RetroArchCore _plusGx = null!;
    private InjectSettingsService _settings = null!;

    [TestInitialize]
    public void Setup()
    {
        SynchronizationContext.SetSynchronizationContext(new InlineSynchronizationContext());
        _bases = new InjectBaseService().Add(Base(SourceConsole.Nes, 0x1000, "Nes Base"));
        _picodrive = Core("picodrive", "PicoDrive");
        _plusGx = Core("genesis_plus_gx", "Genesis Plus GX", recommended: true);
        _cores = new FakeRetroArchCores().Add(_picodrive).Add(_plusGx);
        _dialogs = new InjectDialogService();
        _factory = new RecordingInjectionServiceFactory();
        _history = new FakeInjectionHistory();
        _settings = new InjectSettingsService();
    }

    [TestMethod]
    public void AromaWarnings_AromaMissing_SaysSo()
    {
        var sd = TempFolder();
        try
        {
            _settings.Current = _settings.Current with { SdPath = sd };

            var vm = Genesis();

            Assert.IsTrue(vm.HasAromaWarnings);
            StringAssert.Contains(vm.AromaWarnings.Single(), "Aroma");
            StringAssert.Contains(vm.AromaWarnings.Single(), sd);
        }
        finally
        {
            Directory.Delete(sd, recursive: true);
        }
    }

    [TestMethod]
    public void AromaWarnings_AromaWithSigPatches_Empty()
    {
        var sd = TempFolder();
        try
        {
            _settings.Current = _settings.Current with { SdPath = sd };
            WriteAroma(sd, sigPatches: true);

            var vm = Genesis();

            Assert.IsFalse(vm.HasAromaWarnings);
            Assert.AreEqual(0, vm.AromaWarnings.Count);
        }
        finally
        {
            Directory.Delete(sd, recursive: true);
        }
    }

    [TestMethod]
    public void AromaWarnings_NesConsole_EmptyEvenWithoutAroma()
    {
        var sd = TempFolder();
        try
        {
            _settings.Current = _settings.Current with { SdPath = sd };

            var vm = Create();
            vm.Step = InjectViewModel.Steps.Count;

            Assert.AreEqual(SourceConsole.Nes, vm.SelectedConsole);
            Assert.IsFalse(vm.HasAromaWarnings);
        }
        finally
        {
            Directory.Delete(sd, recursive: true);
        }
    }

    [TestMethod]
    public void AromaWarnings_SdPathUnset_Empty()
    {
        var vm = Genesis();

        Assert.IsFalse(vm.HasAromaWarnings);
        Assert.AreEqual(0, vm.AromaWarnings.Count);
    }

    [TestMethod]
    public void AromaWarnings_SigPatchesMissing_NamesTheModule()
    {
        var sd = TempFolder();
        try
        {
            _settings.Current = _settings.Current with { SdPath = sd };
            WriteAroma(sd, sigPatches: false);

            var vm = Genesis();

            StringAssert.Contains(vm.AromaWarnings.Single(), "01_sigpatches.rpx");
            StringAssert.Contains(vm.AromaWarnings.Single(), InjectViewModel.SigPatchesUrl);
        }
        finally
        {
            Directory.Delete(sd, recursive: true);
        }
    }

    [TestMethod]
    public void AromaWarnings_StepSetToReviewAfterTheCardGainedTheFiles_Refreshes()
    {
        var sd = TempFolder();
        try
        {
            _settings.Current = _settings.Current with { SdPath = sd };
            var vm = Genesis();
            Assert.IsTrue(vm.HasAromaWarnings);

            WriteAroma(sd, sigPatches: true);
            vm.Step = InjectViewModel.Steps.Count;

            Assert.IsFalse(vm.HasAromaWarnings);
        }
        finally
        {
            Directory.Delete(sd, recursive: true);
        }
    }

    [TestMethod]
    public void CanInject_GenesisNoCoreSelected_False()
    {
        var vm = Ready();

        vm.SelectedCore = null;

        Assert.IsFalse(vm.HasTemplate);
        Assert.IsFalse(vm.CanInject);
        Assert.AreEqual("Not selected", vm.ReviewTemplate);
    }

    [TestMethod]
    public void CanInject_GenesisRomAndNameWithCore_True()
    {
        var vm = Genesis();
        Assert.IsFalse(vm.CanInject, "no ROM yet");

        vm.RomPath = GenesisRom;
        Assert.IsFalse(vm.CanInject, "no name yet");

        vm.Name = "Sonic";

        Assert.IsTrue(vm.HasTemplate);
        Assert.IsTrue(vm.CanInject);
        Assert.IsTrue(vm.InjectCommand.CanExecute(null));
        Assert.IsNull(vm.RomFitHint);
    }

    [TestMethod]
    public async Task Inject_Genesis_PassesTheCoreAndRemembersIt()
    {
        var vm = Ready();

        await vm.InjectCommand.ExecuteAsync(null);

        var injection = _factory.Service.Received!;
        Assert.AreSame(_plusGx, injection.Template);
        Assert.AreSame(_plusGx, injection.Core);
        Assert.IsNull(injection.Base);
        Assert.AreEqual(SourceConsole.Genesis, injection.Console);
        Assert.AreEqual(GenesisRom, injection.Rom.Path);
        Assert.IsNull(injection.Options);
        var record = _history.Added.Single().Record;
        Assert.IsTrue(record.Template.IsCore);
        Assert.AreEqual(_plusGx.Id, record.Template.CoreId);
        Assert.AreEqual(SourceConsole.Genesis, record.Console);
        Assert.AreEqual(0, _dialogs.Errors.Count);
    }

    [TestMethod]
    public void Load_CoreRecord_SelectsThatCoreAndLandsOnReview()
    {
        var vm = Create();

        vm.Load(Record(_picodrive.Id));

        Assert.AreEqual(InjectViewModel.Steps.Count, vm.Step);
        Assert.AreEqual(SourceConsole.Genesis, vm.SelectedConsole);
        Assert.AreSame(_picodrive, vm.SelectedCore);
        Assert.AreEqual(GenesisRom, vm.RomPath);
        Assert.AreEqual("Sonic", vm.Name);
        Assert.IsTrue(vm.CanInject);
    }

    [TestMethod]
    public void Load_UnknownCoreId_KeepsTheDefaultCore()
    {
        var vm = Create();

        vm.Load(Record("gone_core"));

        Assert.AreEqual(SourceConsole.Genesis, vm.SelectedConsole);
        Assert.AreSame(_plusGx, vm.SelectedCore);
        Assert.AreEqual(InjectViewModel.Steps.Count, vm.Step);
    }

    [TestMethod]
    public void SelectedConsole_BackToNes_DropsTheCores()
    {
        var vm = Genesis();

        vm.SelectedConsole = SourceConsole.Nes;

        Assert.IsFalse(vm.IsRetroArch);
        Assert.AreEqual(0, vm.Cores.Count);
        Assert.IsNull(vm.SelectedCore);
        Assert.AreEqual("Base", vm.ReviewTemplateLabel);
        Assert.IsNull(vm.RetroArchHint);
        Assert.AreEqual(1, vm.Bases.Count);
        Assert.IsNotNull(vm.SelectedBase);
        Assert.AreEqual("Nes Base (UnitedStates)", vm.ReviewTemplate);
    }

    [TestMethod]
    public void SelectedConsole_Genesis_OffersCoresInsteadOfBases()
    {
        var vm = Genesis();

        Assert.IsTrue(vm.IsRetroArch);
        CollectionAssert.AreEqual(new[] { _picodrive, _plusGx }, vm.Cores.ToArray());
        Assert.AreSame(_plusGx, vm.SelectedCore, "the recommended core is picked first");
        Assert.AreEqual(0, vm.Bases.Count);
        Assert.IsNull(vm.SelectedBase);
        Assert.IsNull(vm.BaseHint);
        Assert.AreEqual("Core", vm.ReviewTemplateLabel);
        StringAssert.Contains(vm.ReviewTemplate, "Genesis Plus GX");
        StringAssert.Contains(vm.ReviewTemplate, "RetroArch");
        Assert.AreEqual(InjectViewModel.RetroArchQuitHint, vm.RetroArchHint);
        StringAssert.Contains(vm.RomExtensions, ".md");
        Assert.AreEqual("Sega Genesis", vm.SelectedConsoleName);
    }

    [TestMethod]
    public void RomExtensions_RetroArchConsole_ComeFromTheCatalog()
    {
        _cores.Systems.Add(new RetroArchSystem(SourceConsole.Genesis, ".sms", ".gg"));
        var vm = Create();

        vm.SelectedConsole = SourceConsole.Genesis;

        Assert.AreEqual(".sms, .gg", vm.RomExtensions);
    }

    [TestMethod]
    public void SelectedConsole_GenesisWithoutCores_HasNoTemplate()
    {
        _cores.Cores.Clear();

        var vm = Genesis();

        Assert.IsFalse(vm.IsRetroArch);
        Assert.IsNull(vm.SelectedCore);
        Assert.IsFalse(vm.HasTemplate);
        StringAssert.Contains(vm.BaseHint, "No base");
    }

    private static string TempFolder()
    {
        var root = Path.Combine(Path.GetTempPath(), "WiiUVirtualConsoleInjector.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void WriteAroma(string sd, bool sigPatches)
    {
        var aroma = Path.Combine(sd, "wiiu", "environments", "aroma");
        Directory.CreateDirectory(aroma);
        if (!sigPatches)
            return;
        var setup = Path.Combine(aroma, "modules", "setup");
        Directory.CreateDirectory(setup);
        File.WriteAllBytes(Path.Combine(setup, "01_sigpatches.rpx"), new byte[] { 0x7F, 0x45, 0x4C, 0x46 });
    }

    private InjectViewModel Create() =>
        new(_bases, _cores, _dialogs, _factory, _settings, _navigation, new FakeSdCard(), new ArtworkBuilderViewModel(new FakeArtworkComposer(), _dialogs, () => _settings.WorkPath), new FakeSoundPlayer(), _history, new FakeCompatibilityLists(), new FakeCommunityArtwork());

    private InjectViewModel Genesis()
    {
        var vm = Create();
        vm.SelectedConsole = SourceConsole.Genesis;
        return vm;
    }

    private InjectViewModel Ready()
    {
        var vm = Genesis();
        vm.RomPath = GenesisRom;
        vm.Name = "Sonic";
        return vm;
    }

    private static InjectionRecord Record(string coreId) =>
        new("abc", DateTimeOffset.Now, SourceConsole.Genesis, TemplateKey.Core(coreId), GenesisRom, "Sonic",
            new TitleIdentity(new TitleId(TitleType.Demo, 0x31323334), new GroupId(0x3456), new ProductCode(ProductCode.EShop, "SONC")));
}
