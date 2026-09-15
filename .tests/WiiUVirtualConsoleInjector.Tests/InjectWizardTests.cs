using PD.WiiU.VirtualConsole;
using WiiUSharp;
using WiiUVirtualConsoleInjector.Services;
using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class InjectWizardTests
{
    private static readonly BaseTitle NesBase = new(new TitleId(TitleType.Game, 0x10101900), "NES Base", Region.UnitedStates, SourceConsole.Nes);
    private static readonly BaseTitle SnesBase = new(new TitleId(TitleType.Game, 0x10111900), "SNES Base", Region.Europe, SourceConsole.Snes);

    private InjectFakes.InjectBaseService _bases = null!;
    private InjectFakes.InjectDialogService _dialogs = null!;
    private InjectFakes.RecordingInjectionServiceFactory _factory = null!;
    private InjectFakes.InjectSettingsService _settings = null!;
    private NavigationService _navigation = null!;

    [TestInitialize]
    public void Initialize()
    {
        _bases = new InjectFakes.InjectBaseService().Add(NesBase).Add(SnesBase, BaseStatus.Downloadable);
        _dialogs = new InjectFakes.InjectDialogService();
        _factory = new InjectFakes.RecordingInjectionServiceFactory();
        _settings = new InjectFakes.InjectSettingsService();
        _navigation = new NavigationService();
    }

    [TestMethod]
    public void Steps_Default_StartsOnConsoleWithSixSteps()
    {
        var vm = Create();

        Assert.AreEqual(6, InjectViewModel.Steps.Count);
        Assert.AreEqual(1, vm.Step);
        Assert.IsTrue(vm.IsConsoleStep);
        Assert.AreEqual("Console", vm.CurrentWizardStep.Label);
        Assert.IsFalse(vm.CanGoPrevious);
        Assert.IsTrue(vm.CanGoNext);
        Assert.IsFalse(vm.PreviousStepCommand.CanExecute(null));
    }

    [TestMethod]
    public void SelectedConsole_OnConsoleStep_AdvancesToBase()
    {
        var vm = Create();

        vm.SelectedConsole = SourceConsole.Snes;

        Assert.AreEqual(2, vm.Step);
        Assert.IsTrue(vm.IsBaseStep);
        Assert.AreEqual("SNES", vm.SelectedConsoleName);
        Assert.AreEqual(".sfc, .smc", vm.RomExtensions);
    }

    [TestMethod]
    public void SelectedBase_UsableOnBaseStep_AdvancesToGame()
    {
        var vm = Create();
        vm.Step = 2;
        vm.SelectedBase = null;

        vm.SelectedBase = vm.Bases.Single();

        Assert.AreEqual(3, vm.Step);
        Assert.IsTrue(vm.IsGameStep);
    }

    [TestMethod]
    public void SelectedBase_NotDownloaded_StaysOnBaseStepWithHint()
    {
        var vm = Create();
        vm.SelectedConsole = SourceConsole.Snes;
        vm.Step = 2;

        vm.SelectedBase = vm.Bases.Single();

        Assert.AreEqual(2, vm.Step);
        Assert.IsFalse(vm.SelectedBase!.IsUsable);
        StringAssert.Contains(vm.BaseHint, "not downloaded");
    }

    [TestMethod]
    public void NextAndPrevious_WalkTheSteps_AndClampAtTheEnds()
    {
        var vm = Create();

        for (var i = 0; i < 10; i++)
            if (vm.NextStepCommand.CanExecute(null))
                vm.NextStepCommand.Execute(null);
        Assert.AreEqual(6, vm.Step);
        Assert.IsTrue(vm.IsReviewStep);
        Assert.IsFalse(vm.CanGoNext);

        vm.PreviousStepCommand.Execute(null);
        Assert.AreEqual(5, vm.Step);
        Assert.IsTrue(vm.IsOptionsStep);

        vm.GoToStepCommand.Execute(InjectViewModel.Steps[3]);
        Assert.IsTrue(vm.IsArtworkStep);
        vm.GoToStepCommand.Execute(null);
        Assert.IsTrue(vm.IsArtworkStep);
    }

    [TestMethod]
    public void Review_Lines_SummariseTheChoices()
    {
        var vm = Create();
        vm.SelectedBase = vm.Bases.Single();
        vm.RomPath = @"C:\roms\mario.nes";
        vm.Name = " Super Mario Bros. ";
        vm.ProductId = "SMB1";

        Assert.AreEqual("NES Base (UnitedStates)", vm.ReviewBase);
        Assert.AreEqual(@"C:\roms\mario.nes", vm.ReviewRom);
        Assert.AreEqual("Super Mario Bros. \u00b7 #SMB1", vm.ReviewGame);

        vm.ClearRomCommand.Execute(null);
        Assert.AreEqual("Not selected", vm.ReviewRom);
        Assert.IsFalse(vm.HasRom);
        Assert.IsFalse(vm.ClearRomCommand.CanExecute(null));
        vm.Name = null;
        Assert.AreEqual("Not named \u00b7 #SMB1", vm.ReviewGame);
    }

    [TestMethod]
    public void ManageBases_Always_AsksTheShellForTheBasesPage()
    {
        Type? requested = null;
        _navigation.Requested += (_, type) => requested = type;
        var vm = Create();

        vm.ManageBasesCommand.Execute(null);

        Assert.AreEqual(typeof(BasesViewModel), requested);
    }

    [TestMethod]
    public void Refresh_MissingKeys_MarksBasesUnusable()
    {
        _factory.Missing = _ => new[] { "Wii U common key" };
        var vm = Create();
        vm.Step = 2;

        vm.SelectedBase = vm.Bases.Single();

        Assert.IsTrue(vm.SelectedBase!.IsPresent);
        Assert.IsFalse(vm.SelectedBase.KeysOk);
        Assert.IsFalse(vm.SelectedBase.IsUsable);
        Assert.AreEqual(2, vm.Step, "a locked base does not advance");
        StringAssert.Contains(vm.BaseHint, "key");
        Assert.AreEqual("UnitedStates", vm.SelectedBase.Region);
        Assert.AreEqual("0005000010101900", vm.SelectedBase.TitleId);
    }

    private InjectViewModel Create() => new(_bases, _dialogs, _factory, _settings, _navigation, new FakeSdCard(), new ArtworkBuilderViewModel(new FakeArtworkComposer(), _dialogs, new FakeUiScheduler(), () => _settings.WorkPath), new FakeSoundPlayer());
}
