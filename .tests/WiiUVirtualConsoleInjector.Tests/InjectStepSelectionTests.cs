using System.ComponentModel;
using WiiUVirtualConsoleInjector.Services;
using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class InjectStepSelectionTests
{
    [TestMethod]
    public void SelectedStep_Default_IsTheCurrentWizardStep()
    {
        var vm = Create();

        Assert.AreSame(vm.CurrentWizardStep, vm.SelectedStep);
        Assert.AreEqual(1, vm.SelectedStep!.Number);
    }

    [TestMethod]
    public void SelectedStep_SetToAStep_JumpsThere()
    {
        var vm = Create();

        vm.SelectedStep = InjectViewModel.Steps[3];

        Assert.AreEqual(4, vm.Step);
        Assert.IsTrue(vm.IsArtworkStep);
        Assert.AreSame(InjectViewModel.Steps[3], vm.SelectedStep);
    }

    [TestMethod]
    public void SelectedStep_SetToNull_KeepsTheStep()
    {
        var vm = Create();
        vm.Step = 3;

        vm.SelectedStep = null;

        Assert.AreEqual(3, vm.Step);
    }

    [TestMethod]
    public void Step_Changed_NotifiesSelectedStep()
    {
        var vm = Create();
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.Step = 5;

        CollectionAssert.Contains(changed, nameof(InjectViewModel.SelectedStep));
        CollectionAssert.Contains(changed, nameof(InjectViewModel.CurrentWizardStep));
    }

    private static InjectViewModel Create() =>
        new(new InjectFakes.InjectBaseService(), new InjectFakes.InjectDialogService(), new InjectFakes.RecordingInjectionServiceFactory(), new InjectFakes.InjectSettingsService(), new NavigationService(), new FakeSdCard(), new ArtworkBuilderViewModel(new FakeArtworkComposer(), new InjectFakes.InjectDialogService(), () => Path.GetTempPath()), new FakeSoundPlayer(), new FakeInjectionHistory(), new FakeCompatibilityLists(), new FakeCommunityArtwork());
}
