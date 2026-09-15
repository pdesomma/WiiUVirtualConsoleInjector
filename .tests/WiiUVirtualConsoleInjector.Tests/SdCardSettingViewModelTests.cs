using PD.WiiU.VirtualConsole;
using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class SdCardSettingViewModelTests
{
    private static readonly RemovableDrive Card = new(@"E:\", "WIIU", 8589934592, 16106127360, true);
    private static readonly RemovableDrive Stick = new(@"F:\", "STICK", 1073741824, 4294967296, false);

    private FakeLinkOpener _links = null!;
    private FakeSdCard _sdCard = null!;
    private FakeSettingsService _settings = null!;

    [TestInitialize]
    public void Setup()
    {
        _links = new FakeLinkOpener();
        _sdCard = new FakeSdCard();
        _settings = new FakeSettingsService();
    }

    [TestMethod]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new SdCardSettingViewModel(null!, _settings, _links));
        Assert.ThrowsExactly<ArgumentNullException>(() => new SdCardSettingViewModel(_sdCard, null!, _links));
        Assert.ThrowsExactly<ArgumentNullException>(() => new SdCardSettingViewModel(_sdCard, _settings, null!));
    }

    [TestMethod]
    public void Constructor_NoDrives_HasNoCardAndSaysSo()
    {
        var vm = new SdCardSettingViewModel(_sdCard, _settings, _links);

        Assert.AreEqual(0, vm.Drives.Count);
        Assert.IsNull(vm.SelectedDrive);
        Assert.IsFalse(vm.HasCard);
        Assert.AreEqual(SdCardSettingViewModel.NoDriveText, vm.Status);
    }

    [TestMethod]
    public void Constructor_DetectedDrive_SelectsItWithoutSavingAnything()
    {
        _sdCard.Removable.AddRange(new[] { Stick, Card });
        _settings.DetectedSdPath = Card.RootPath;

        var vm = new SdCardSettingViewModel(_sdCard, _settings, _links);

        Assert.AreSame(Card, vm.SelectedDrive);
        Assert.IsTrue(vm.HasCard);
        Assert.AreEqual(Path.Combine(@"E:\", SdCard.InstallFolder), vm.Status);
        Assert.AreEqual(0, _settings.Saves, "detection is not a choice the user made");
    }

    [TestMethod]
    public void SelectedDrive_Chosen_SavesTheRootPath()
    {
        _sdCard.Removable.AddRange(new[] { Stick, Card });
        var vm = new SdCardSettingViewModel(_sdCard, _settings, _links);

        vm.SelectedDrive = Stick;

        Assert.AreEqual(@"F:\", _settings.Current.SdPath);
        Assert.AreEqual(1, _settings.Saves);
        Assert.AreEqual(Path.Combine(@"F:\", SdCard.InstallFolder), vm.Status);
    }

    [TestMethod]
    public void RescanCommand_DriveArrived_ListsItAndKeepsTheSavedChoice()
    {
        var vm = new SdCardSettingViewModel(_sdCard, _settings, _links);
        _sdCard.Removable.Add(Card);
        _settings.Current = _settings.Current with { SdPath = Card.RootPath };

        vm.RescanCommand.Execute(null);

        CollectionAssert.AreEqual(new[] { Card }, vm.Drives);
        Assert.AreSame(Card, vm.SelectedDrive);
        Assert.AreEqual(0, _settings.Saves, "rescanning saves nothing of its own");
    }

    [TestMethod]
    public void CopyAfterInject_Toggled_SavesOnceAndSticks()
    {
        var vm = new SdCardSettingViewModel(_sdCard, _settings, _links);

        vm.CopyAfterInject = true;
        vm.CopyAfterInject = true;

        Assert.IsTrue(_settings.Current.CopyToSdCard);
        Assert.AreEqual(1, _settings.Saves);
    }

    [TestMethod]
    public async Task OpenCommand_WithAndWithoutACard_OpensOnlyWhenThereIsOne()
    {
        var vm = new SdCardSettingViewModel(_sdCard, _settings, _links);
        await vm.OpenCommand.ExecuteAsync(null);
        Assert.AreEqual(0, _links.Folders.Count);

        _sdCard.Removable.Add(Card);
        vm.RescanCommand.Execute(null);
        vm.SelectedDrive = Card;
        await vm.OpenCommand.ExecuteAsync(null);

        CollectionAssert.AreEqual(new[] { Path.Combine(@"E:\", SdCard.InstallFolder) }, _links.Folders);
    }
}
