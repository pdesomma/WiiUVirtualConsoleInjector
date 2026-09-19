using PD.WiiU.VirtualConsole;
using WiiUSharp;
using WiiUVirtualConsoleInjector.Services;
using WiiUVirtualConsoleInjector.ViewModels;
using WiiUVirtualConsoleInjector.ViewModels.Options;
using static WiiUVirtualConsoleInjector.Tests.InjectFakes;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class InjectRetroArchTests
{
    private const string GenesisRom = @"C:\sonic.md";
    private static readonly BiosFile PsxBios = new("PlayStation BIOS", "scph5501.bin", "scph5500.bin", "scph1001.bin");

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
        _plusGx = Core("genesis_plus_gx", "Genesis Plus GX");
        _cores = new FakeRetroArchCores().Add(_plusGx).Add(_picodrive);
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
    public void AromaWarnings_BiosMissing_NamesTheFileAndFolder()
    {
        var sd = TempFolder();
        try
        {
            _settings.Current = _settings.Current with { SdPath = sd };
            WriteAroma(sd, sigPatches: true);
            _cores.Add(Core("handy", "Handy", SourceConsole.AtariLynx, recommended: true));
            _cores.Systems.Add(new RetroArchSystem(SourceConsole.AtariLynx, ".lnx") { BiosFiles = new[] { new BiosFile("lynxboot.img") } });
            var vm = Create();

            vm.SelectedConsole = SourceConsole.AtariLynx;

            Assert.AreEqual(1, vm.AromaWarnings.Count);
            StringAssert.Contains(vm.AromaWarnings[0], "lynxboot.img");
            StringAssert.Contains(vm.AromaWarnings[0], RetroArchSystem.SystemFolder);
            StringAssert.Contains(vm.BiosHint, "lynxboot.img");

            Directory.CreateDirectory(Path.Combine(sd, "retroarch", "system"));
            File.WriteAllBytes(Path.Combine(sd, "retroarch", "system", "lynxboot.img"), new byte[] { 1 });
            vm.Step = InjectViewModel.Steps.Count;

            Assert.AreEqual(0, vm.AromaWarnings.Count, "the review step re-checks the card");
        }
        finally
        {
            Directory.Delete(sd, recursive: true);
        }
    }

    [TestMethod]
    public void BiosFiles_LynxWithAPick_ClearsTheWarningAndListsTheCopy()
    {
        var sd = TempFolder();
        try
        {
            _settings.Current = _settings.Current with { SdPath = sd };
            WriteAroma(sd, sigPatches: true);
            var vm = Lynx();
            var bios = vm.BiosFiles.Single();
            Assert.IsTrue(bios.IsWanted);
            Assert.IsFalse(bios.IsOnCard);
            Assert.AreEqual(1, vm.AromaWarnings.Count);
            Assert.AreEqual(0, vm.CardFiles.Count);

            var dump = Path.Combine(sd, "lynxboot.img");
            File.WriteAllBytes(dump, new byte[512]);
            bios.Field.Path = dump;
            vm.Step = InjectViewModel.Steps.Count;

            Assert.IsFalse(bios.IsWanted);
            Assert.AreEqual(0, vm.AromaWarnings.Count, "a picked copy answers the warning");
            Assert.IsTrue(vm.HasReviewExtras);
            StringAssert.Contains(vm.ReviewExtras.Single(), "lynxboot.img");
            StringAssert.Contains(vm.ReviewExtras.Single(), "SD:/retroarch/system/lynxboot.img");
            StringAssert.Contains(vm.ReviewExtras.Single(), new ByteSize(512).ToString());
            Assert.AreEqual("retroarch/system/lynxboot.img", vm.CardFiles.Single().CardPath);
        }
        finally
        {
            Directory.Delete(sd, recursive: true);
        }
    }

    [TestMethod]
    public void BiosFiles_AlreadyOnTheCard_NoWarningAndNoCopyEvenWhenPicked()
    {
        var sd = TempFolder();
        try
        {
            _settings.Current = _settings.Current with { SdPath = sd };
            WriteAroma(sd, sigPatches: true);
            Directory.CreateDirectory(Path.Combine(sd, "retroarch", "system"));
            File.WriteAllBytes(Path.Combine(sd, "retroarch", "system", "lynxboot.img"), new byte[] { 1 });
            var vm = Lynx();

            vm.BiosFiles.Single().Field.Path = Path.Combine(sd, "retroarch", "system", "lynxboot.img");
            vm.Step = InjectViewModel.Steps.Count;

            Assert.IsTrue(vm.BiosFiles.Single().IsOnCard);
            Assert.AreEqual(0, vm.AromaWarnings.Count);
            Assert.IsFalse(vm.HasReviewExtras);
            StringAssert.Contains(vm.BiosFiles.Single().Status, "Already");
        }
        finally
        {
            Directory.Delete(sd, recursive: true);
        }
    }

    [TestMethod]
    public async Task Inject_LynxWithABiosPick_CopiesItToTheCardAndRemembersIt()
    {
        var sd = TempFolder();
        try
        {
            _settings.Current = _settings.Current with { SdPath = sd, CopyToSdCard = true };
            WriteAroma(sd, sigPatches: true);
            var dump = Path.Combine(sd, "lynxboot.img");
            File.WriteAllBytes(dump, new byte[16]);
            var card = new FakeSdCard();
            var vm = Lynx(card);
            vm.BiosFiles.Single().Field.Path = dump;
            vm.RomPath = @"C:\game.lnx";
            vm.Name = "Slime World";

            await vm.InjectCommand.ExecuteAsync(null);

            Assert.AreEqual(1, card.Copies.Count, "the title");
            Assert.AreEqual(1, card.FileCopies.Count, "then the BIOS");
            Assert.AreEqual("retroarch/system/lynxboot.img", card.FileCopies[0].File.CardPath);
            Assert.AreEqual(sd, card.FileCopies[0].Root);
            var record = _history.Added.Single().Record;
            Assert.AreEqual(dump, record.CardFiles.Single().SourcePath);
            Assert.AreEqual(0, _dialogs.Errors.Count);
        }
        finally
        {
            Directory.Delete(sd, recursive: true);
        }
    }

    [TestMethod]
    public async Task Inject_CopyToCardOff_LeavesTheBiosAlone()
    {
        var card = new FakeSdCard();
        var vm = Lynx(card);
        vm.BiosFiles.Single().Field.Path = @"C:\dumps\lynxboot.img";
        vm.RomPath = @"C:\game.lnx";
        vm.Name = "Slime World";

        await vm.InjectCommand.ExecuteAsync(null);

        Assert.AreEqual(0, card.FileCopies.Count);
        Assert.AreEqual(1, _history.Added.Single().Record.CardFiles.Count, "still remembered for a rebuild");
    }

    [TestMethod]
    public void Load_RecordWithCardFiles_RestoresThePick()
    {
        var vm = Lynx();
        var record = new InjectionRecord("abc", DateTimeOffset.Now, SourceConsole.AtariLynx, TemplateKey.Core("handy"), @"C:\game.lnx", "Slime World",
            new TitleIdentity(new TitleId(TitleType.Demo, 0x31323334), new GroupId(0x3456), new ProductCode(ProductCode.EShop, "SLIM")))
        {
            CardFiles = new[] { new CardFile(@"C:\dumps\lynxboot.img", "retroarch/system/lynxboot.img") },
        };

        vm.Load(record);

        Assert.AreEqual(@"C:\dumps\lynxboot.img", vm.BiosFiles.Single().Field.Path);
        Assert.AreEqual(1, vm.CardFiles.Count);
    }

    [TestMethod]
    public void BiosHint_ConsoleWithoutBios_Null()
    {
        var vm = Genesis();

        Assert.IsNull(vm.BiosHint);
    }

    [TestMethod]
    public void BiosHint_MultiNameBios_ListsTheLabelAndEveryName()
    {
        var vm = PlayStation();

        StringAssert.Contains(vm.BiosHint, "PlayStation BIOS (one of scph5501.bin, scph5500.bin, scph1001.bin)");
        StringAssert.Contains(vm.BiosHint, RetroArchSystem.SystemFolder);
    }

    [TestMethod]
    public void BiosFiles_MultiNameBiosOnCardUnderTheSecondName_OnCardAsThatName()
    {
        var sd = TempFolder();
        try
        {
            _settings.Current = _settings.Current with { SdPath = sd };
            WriteAroma(sd, sigPatches: true);
            Directory.CreateDirectory(Path.Combine(sd, "retroarch", "system"));
            File.WriteAllBytes(Path.Combine(sd, "retroarch", "system", "scph5500.bin"), new byte[] { 1 });

            var vm = PlayStation();
            var bios = vm.BiosFiles.Single();

            Assert.IsTrue(bios.IsOnCard);
            Assert.AreEqual("scph5500.bin", bios.OnCardAs);
            Assert.IsFalse(bios.IsWanted);
            Assert.AreEqual(0, vm.AromaWarnings.Count);
            StringAssert.Contains(bios.Status, "scph5500.bin");
        }
        finally
        {
            Directory.Delete(sd, recursive: true);
        }
    }

    [TestMethod]
    public void BiosFiles_MultiNameBiosMissing_WarningNamesTheLabelAndEveryName()
    {
        var sd = TempFolder();
        try
        {
            _settings.Current = _settings.Current with { SdPath = sd };
            WriteAroma(sd, sigPatches: true);

            var vm = PlayStation();

            Assert.IsNull(vm.BiosFiles.Single().OnCardAs);
            Assert.IsTrue(vm.BiosFiles.Single().IsWanted);
            StringAssert.Contains(vm.AromaWarnings.Single(), "PlayStation BIOS (one of scph5501.bin, scph5500.bin, scph1001.bin)");
        }
        finally
        {
            Directory.Delete(sd, recursive: true);
        }
    }

    [TestMethod]
    public void BiosFiles_PickNamedLikeAnAcceptedName_SelectsThatName()
    {
        var vm = PlayStation();
        var bios = vm.BiosFiles.Single();

        bios.Field.Path = @"C:\dumps\SCPH1001.BIN";

        Assert.AreEqual("scph1001.bin", bios.SelectedName);
        Assert.AreEqual("retroarch/system/scph1001.bin", vm.CardFiles.Single().CardPath);
    }

    [TestMethod]
    public void BiosFiles_PickWithAnUnrelatedName_SelectsThePreferredName()
    {
        var vm = PlayStation();
        var bios = vm.BiosFiles.Single();
        bios.SelectedName = "scph5500.bin";

        bios.Field.Path = @"C:\dumps\psx-bios-us.bin";

        Assert.AreEqual("scph5501.bin", bios.SelectedName);
        Assert.AreEqual("retroarch/system/scph5501.bin", vm.CardFiles.Single().CardPath);
    }

    [TestMethod]
    public void BiosFiles_SelectedNameChanged_CardPathFollows()
    {
        var vm = PlayStation();
        var bios = vm.BiosFiles.Single();
        bios.Field.Path = @"C:\dumps\psx-bios-us.bin";

        bios.SelectedName = "scph1001.bin";

        Assert.AreEqual("retroarch/system/scph1001.bin", bios.CardPath);
        Assert.AreEqual("retroarch/system/scph1001.bin", vm.CardFiles.Single().CardPath);
        StringAssert.Contains(vm.ReviewExtras.Single(), "SD:/retroarch/system/scph1001.bin");
    }

    [TestMethod]
    public void Load_RecordWithABiosUnderAnotherAcceptedName_RestoresThePickAndTheName()
    {
        var vm = PlayStation();
        var record = new InjectionRecord("abc", DateTimeOffset.Now, SourceConsole.PlayStation, TemplateKey.Core("pcsx_rearmed"), @"C:\game.cue", "Crash",
            new TitleIdentity(new TitleId(TitleType.Demo, 0x31323334), new GroupId(0x3456), new ProductCode(ProductCode.EShop, "CRSH")))
        {
            CardFiles = new[] { new CardFile(@"C:\dumps\psx-bios-us.bin", "retroarch/system/SCPH1001.BIN") },
        };

        vm.Load(record);

        var bios = vm.BiosFiles.Single();
        Assert.AreEqual(@"C:\dumps\psx-bios-us.bin", bios.Field.Path);
        Assert.AreEqual("scph1001.bin", bios.SelectedName);
        Assert.AreEqual("retroarch/system/scph1001.bin", vm.CardFiles.Single().CardPath);
    }

    [TestMethod]
    public void Load_RecordWithAForeignCardFile_LeavesTheRowEmpty()
    {
        var vm = PlayStation();
        var record = new InjectionRecord("abc", DateTimeOffset.Now, SourceConsole.PlayStation, TemplateKey.Core("pcsx_rearmed"), @"C:\game.cue", "Crash",
            new TitleIdentity(new TitleId(TitleType.Demo, 0x31323334), new GroupId(0x3456), new ProductCode(ProductCode.EShop, "CRSH")))
        {
            CardFiles = new[] { new CardFile(@"C:\dumps\lynxboot.img", "retroarch/system/lynxboot.img") },
        };

        vm.Load(record);

        Assert.IsNull(vm.BiosFiles.Single().Field.Path);
        Assert.AreEqual("scph5501.bin", vm.BiosFiles.Single().SelectedName);
        Assert.AreEqual(0, vm.CardFiles.Count);
    }

    [TestMethod]
    public void Refresh_SameConsoleAgain_KeepsThePickAndTheName()
    {
        var vm = PlayStation();
        var bios = vm.BiosFiles.Single();
        bios.Field.Path = @"C:\dumps\psx-bios-us.bin";
        bios.SelectedName = "scph1001.bin";

        vm.ActivateAsync().GetAwaiter().GetResult();

        Assert.AreNotSame(bios, vm.BiosFiles.Single());
        Assert.AreEqual(@"C:\dumps\psx-bios-us.bin", vm.BiosFiles.Single().Field.Path);
        Assert.AreEqual("scph1001.bin", vm.BiosFiles.Single().SelectedName);
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
        CollectionAssert.AreEqual(new[] { _plusGx, _picodrive }, vm.Cores.ToArray());
        Assert.AreSame(_plusGx, vm.SelectedCore, "the first core is the default");
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
        Assert.IsFalse(vm.HasBothModes);
        Assert.IsNull(vm.SelectedCore);
        Assert.IsFalse(vm.HasTemplate);
        StringAssert.Contains(vm.BaseHint, "No base");
    }

    [TestMethod]
    public void SelectedConsole_Genesis_HasOnlyTheCoreMode()
    {
        var vm = Genesis();

        Assert.IsTrue(vm.IsRetroArch);
        Assert.IsFalse(vm.HasBothModes);
        Assert.IsFalse(vm.UseRetroArch, "nothing to switch; the core is the only way");
        Assert.AreEqual(0, vm.Bases.Count);
    }

    [TestMethod]
    public void SelectedConsole_NesWithABaseAndCores_DefaultsToTheBase()
    {
        var vm = NesWithCores();

        Assert.IsFalse(vm.UseRetroArch);
        Assert.IsTrue(vm.UseVirtualConsole);
        Assert.IsFalse(vm.IsRetroArch);
        Assert.IsTrue(vm.HasBothModes);
        Assert.AreEqual(1, vm.Bases.Count);
        Assert.AreEqual(2, vm.Cores.Count);
        Assert.IsNotNull(vm.SelectedBase);
        Assert.IsNotNull(vm.SelectedCore);
        Assert.AreEqual("Base", vm.ReviewTemplateLabel);
        Assert.AreEqual("Nes Base (UnitedStates)", vm.ReviewTemplate);
        Assert.IsNull(vm.RetroArchHint);
        Assert.AreEqual(".nes", vm.RomExtensions);
        Assert.IsInstanceOfType<NesOptionsViewModel>(vm.CurrentOptions);
        Assert.IsTrue(vm.HasTemplate);
    }

    [TestMethod]
    public void UseRetroArch_NesSwitchedToTheCore_EverythingFollows()
    {
        var vm = NesWithCores();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.UseRetroArch = true;

        Assert.IsTrue(vm.IsRetroArch);
        Assert.IsFalse(vm.UseVirtualConsole);
        Assert.IsTrue(vm.HasBothModes);
        Assert.AreEqual("Core", vm.ReviewTemplateLabel);
        StringAssert.Contains(vm.ReviewTemplate, "FCEUmm");
        StringAssert.Contains(vm.ReviewTemplate, "RetroArch");
        Assert.AreEqual(InjectViewModel.RetroArchQuitHint, vm.RetroArchHint);
        Assert.IsNull(vm.BaseHint);
        Assert.AreEqual(".nes, .fds", vm.RomExtensions, "the core's own list");
        Assert.IsInstanceOfType<NoOptionsViewModel>(vm.CurrentOptions);
        Assert.AreEqual(SourceConsole.Nes, vm.CurrentOptions.Console);
        Assert.IsTrue(vm.HasTemplate);
        foreach (var name in new[] { nameof(vm.IsRetroArch), nameof(vm.HasTemplate), nameof(vm.CanInject), nameof(vm.BaseHint), nameof(vm.RetroArchHint), nameof(vm.BiosHint), nameof(vm.ReviewTemplateLabel), nameof(vm.ReviewTemplate), nameof(vm.RomExtensions), nameof(vm.UseVirtualConsole), nameof(vm.CurrentOptions) })
            CollectionAssert.Contains(changed, name, name);

        vm.SelectedCore = null;
        Assert.IsFalse(vm.HasTemplate, "on the core side the base no longer counts");
        Assert.IsFalse(vm.CanInject);
    }

    [TestMethod]
    public void UseRetroArch_NesSwitchedBack_ReturnsToTheBase()
    {
        var vm = NesWithCores();
        vm.UseRetroArch = true;

        vm.UseVirtualConsole = true;

        Assert.IsFalse(vm.UseRetroArch);
        Assert.IsFalse(vm.IsRetroArch);
        Assert.AreEqual("Base", vm.ReviewTemplateLabel);
        Assert.AreEqual(".nes", vm.RomExtensions);
        Assert.IsInstanceOfType<NesOptionsViewModel>(vm.CurrentOptions);
        Assert.IsTrue(vm.HasTemplate);
    }

    [TestMethod]
    public async Task Inject_NesOnTheCore_PassesTheCoreForNesWithoutOptions()
    {
        var vm = NesWithCores();
        vm.UseRetroArch = true;
        vm.RomPath = @"C:\game.nes";
        vm.Name = "Mario";

        await vm.InjectCommand.ExecuteAsync(null);

        var injection = _factory.Service.Received!;
        Assert.IsNotNull(injection.Core);
        Assert.AreSame(injection.Core, injection.Template);
        Assert.AreEqual("fceumm", injection.Core!.Id);
        Assert.AreEqual(SourceConsole.Nes, injection.Core.Console);
        Assert.AreEqual(SourceConsole.Nes, injection.Console);
        Assert.IsNull(injection.Base);
        Assert.IsNull(injection.Options);
        var record = _history.Added.Single().Record;
        Assert.IsTrue(record.Template.IsCore);
        Assert.AreEqual("fceumm", record.Template.CoreId);
        Assert.AreEqual(SourceConsole.Nes, record.Console);
        Assert.AreEqual(0, _dialogs.Confirms.Count);
    }

    [TestMethod]
    public async Task Inject_NesOnTheBaseWithCoresAround_StillPassesTheBase()
    {
        var vm = NesWithCores();
        vm.RomPath = @"C:\game.nes";
        vm.Name = "Mario";

        await vm.InjectCommand.ExecuteAsync(null);

        var injection = _factory.Service.Received!;
        Assert.IsNull(injection.Core);
        Assert.AreSame(vm.SelectedBase!.Base, injection.Base);
        Assert.IsInstanceOfType<PD.WiiU.VirtualConsole.Options.NesOptions>(injection.Options);
        Assert.IsFalse(_history.Added.Single().Record.Template.IsCore);
    }

    [TestMethod]
    public async Task RomFitHint_SwitchingToTheCore_ClearsAndComesBackWithTheBase()
    {
        _factory.Fit = (b, r) => b.Console == SourceConsole.Nes ? "too big" : null;
        var vm = NesWithCores();
        vm.RomPath = @"C:\game.nes";
        vm.Name = "Mario";
        await vm.RomFitCheck;
        Assert.IsTrue(vm.HasRomFitHint);
        Assert.IsFalse(vm.CanInject);

        vm.UseRetroArch = true;
        await vm.RomFitCheck;

        Assert.IsFalse(vm.HasRomFitHint, "a core has no size limit");
        Assert.IsTrue(vm.CanInject);

        vm.UseRetroArch = false;
        await vm.RomFitCheck;

        Assert.IsTrue(vm.HasRomFitHint);
        Assert.IsFalse(vm.CanInject);
    }

    [TestMethod]
    public async Task Inject_SnesOnTheCore_SkipsTheCoProcessorWarning()
    {
        _bases.Add(Base(SourceConsole.Snes, 0x2000, "Snes Base"));
        _cores.Add(Core("snes9x2010", "Snes9x 2010", SourceConsole.Snes, recommended: true));
        var vm = Create();

        vm.SelectedConsole = SourceConsole.Snes;
        vm.RomPath = @"C:\game.sfc";
        vm.Name = "Star Fox";
        vm.UseRetroArch = true;
        await vm.InjectCommand.ExecuteAsync(null);
        Assert.AreEqual(0, _dialogs.Confirms.Count, "Snes9x handles the co-processors");

        // a success starts over, so set the SNES up again
        vm.SelectedConsole = SourceConsole.Snes;
        vm.RomPath = @"C:\game.sfc";
        vm.Name = "Star Fox";
        await vm.InjectCommand.ExecuteAsync(null);
        Assert.AreEqual(1, _dialogs.Confirms.Count, "the base does not");
        StringAssert.Contains(_dialogs.Confirms[0].Message, "Co-Processors");
    }

    [TestMethod]
    public void Load_NesCoreRecord_SwitchesToTheCoreAndSelectsIt()
    {
        var vm = NesWithCores();
        var record = new InjectionRecord("abc", DateTimeOffset.Now, SourceConsole.Nes, TemplateKey.Core("nestopia"), @"C:\game.nes", "Mario",
            new TitleIdentity(new TitleId(TitleType.Demo, 0x31323334), new GroupId(0x3456), new ProductCode(ProductCode.EShop, "MARI")));

        vm.Load(record);

        Assert.IsTrue(vm.UseRetroArch);
        Assert.IsTrue(vm.IsRetroArch);
        Assert.AreEqual("nestopia", vm.SelectedCore!.Id);
        Assert.IsInstanceOfType<NoOptionsViewModel>(vm.CurrentOptions);
        Assert.IsTrue(vm.CanInject);
        Assert.AreEqual(InjectViewModel.Steps.Count, vm.Step);
    }

    [TestMethod]
    public void Load_NesBaseRecord_SwitchesBackToTheBase()
    {
        var vm = NesWithCores();
        vm.UseRetroArch = true;
        var record = new InjectionRecord("abc", DateTimeOffset.Now, SourceConsole.Nes, TemplateKey.Base(_bases.Bases[0].TitleId), @"C:\game.nes", "Mario",
            new TitleIdentity(new TitleId(TitleType.Demo, 0x31323334), new GroupId(0x3456), new ProductCode(ProductCode.EShop, "MARI")));

        vm.Load(record);

        Assert.IsFalse(vm.UseRetroArch);
        Assert.IsFalse(vm.IsRetroArch);
        Assert.AreEqual(_bases.Bases[0].TitleId, vm.SelectedBase!.Base.TitleId);
        Assert.IsInstanceOfType<NesOptionsViewModel>(vm.CurrentOptions);
        Assert.IsTrue(vm.CanInject);
    }

    [TestMethod]
    public void SelectedConsole_ChangedWhileOnTheCore_ResetsToTheBase()
    {
        var vm = NesWithCores();
        vm.UseRetroArch = true;

        vm.SelectedConsole = SourceConsole.Genesis;
        Assert.IsFalse(vm.UseRetroArch);
        Assert.IsTrue(vm.IsRetroArch, "Genesis has no base");
        Assert.IsFalse(vm.HasBothModes);

        vm.SelectedConsole = SourceConsole.Nes;
        Assert.IsFalse(vm.UseRetroArch);
        Assert.IsFalse(vm.IsRetroArch);
        Assert.IsTrue(vm.HasBothModes);
        Assert.IsInstanceOfType<NesOptionsViewModel>(vm.CurrentOptions);
    }

    [TestMethod]
    public void StartOver_OnTheCore_ResetsToTheBase()
    {
        var vm = NesWithCores();
        vm.UseRetroArch = true;

        vm.StartOver();

        Assert.IsFalse(vm.UseRetroArch);
        Assert.IsFalse(vm.IsRetroArch);
        Assert.IsInstanceOfType<NesOptionsViewModel>(vm.CurrentOptions);
    }

    [TestMethod]
    public void BiosFiles_Tg16OnTheBase_NotCheckedOrCopiedUntilTheCoreIsPicked()
    {
        var sd = TempFolder();
        try
        {
            _settings.Current = _settings.Current with { SdPath = sd };
            _bases.Add(Base(SourceConsole.Tg16, 0x3000, "Tg16 Base"));
            _cores.Add(Core("mednafen_pce", "Beetle PCE", SourceConsole.Tg16, recommended: true));
            _cores.Systems.Add(new RetroArchSystem(SourceConsole.Tg16, ".pce", ".cue") { BiosFiles = new[] { new BiosFile("syscard3.pce") } });
            var vm = Create();
            vm.SelectedConsole = SourceConsole.Tg16;
            var dump = Path.Combine(sd, "syscard3.pce");
            File.WriteAllBytes(dump, new byte[16]);
            vm.BiosFiles.Single().Field.Path = dump;
            vm.Step = InjectViewModel.Steps.Count;

            Assert.AreEqual(0, vm.AromaWarnings.Count, "no Aroma needed on the base");
            Assert.AreEqual(0, vm.CardFiles.Count);
            Assert.IsFalse(vm.HasReviewExtras);
            Assert.AreEqual(".pce", vm.RomExtensions);

            vm.UseRetroArch = true;

            Assert.AreEqual(1, vm.AromaWarnings.Count, "now Aroma is missing");
            Assert.AreEqual(1, vm.CardFiles.Count);
            Assert.IsTrue(vm.HasReviewExtras);
            Assert.AreEqual(".pce, .cue", vm.RomExtensions);
            StringAssert.Contains(vm.BiosHint, "syscard3.pce");
        }
        finally
        {
            Directory.Delete(sd, recursive: true);
        }
    }

    private InjectViewModel NesWithCores()
    {
        _cores.Add(Core("fceumm", "FCEUmm", SourceConsole.Nes, recommended: true));
        _cores.Add(Core("nestopia", "Nestopia UE", SourceConsole.Nes));
        _cores.Systems.Add(new RetroArchSystem(SourceConsole.Nes, ".nes", ".fds"));
        return Create();
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

    private InjectViewModel Lynx(FakeSdCard? card = null)
    {
        _cores.Add(Core("handy", "Handy", SourceConsole.AtariLynx, recommended: true));
        _cores.Systems.Add(new RetroArchSystem(SourceConsole.AtariLynx, ".lnx") { BiosFiles = new[] { new BiosFile("lynxboot.img") } });
        var vm = card is null ? Create() : new InjectViewModel(_bases, _cores, _dialogs, _factory, _settings, _navigation, card, new ArtworkBuilderViewModel(new FakeArtworkComposer(), _dialogs, () => _settings.WorkPath), new FakeSoundPlayer(), _history, new FakeCompatibilityLists(), new FakeCommunityArtwork());
        vm.SelectedConsole = SourceConsole.AtariLynx;
        return vm;
    }

    private InjectViewModel PlayStation()
    {
        _cores.Add(Core("pcsx_rearmed", "PCSX-ReARMed", SourceConsole.PlayStation, recommended: true));
        _cores.Systems.Add(new RetroArchSystem(SourceConsole.PlayStation, ".cue", ".chd") { BiosFiles = new[] { PsxBios } });
        var vm = Create();
        vm.SelectedConsole = SourceConsole.PlayStation;
        return vm;
    }

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
