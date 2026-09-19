using PD.WiiU.VirtualConsole;
using WiiUVirtualConsoleInjector.ViewModels;
using static WiiUVirtualConsoleInjector.Tests.InjectFakes;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class BiosFileViewModelTests
{
    private static readonly BiosFile Lynx = new("lynxboot.img");
    private static readonly BiosFile Psx = new("PlayStation BIOS", "scph5501.bin", "scph5500.bin", "scph1001.bin");

    private InjectDialogService _dialogs = null!;

    [TestInitialize]
    public void Setup()
    {
        SynchronizationContext.SetSynchronizationContext(new InlineSynchronizationContext());
        _dialogs = new InjectDialogService();
    }

    [TestMethod]
    public void CardFile_OnCard_Null()
    {
        var vm = new BiosFileViewModel(_dialogs, Lynx);
        vm.Field.Path = @"C:\dumps\lynxboot.img";

        vm.OnCardAs = "lynxboot.img";

        Assert.IsNull(vm.CardFile);
        Assert.IsTrue(vm.IsOnCard);
        Assert.IsFalse(vm.IsWanted);
    }

    [TestMethod]
    public void CardFile_PickedAndNotOnCard_CopiesUnderTheSelectedName()
    {
        var vm = new BiosFileViewModel(_dialogs, Psx);
        vm.Field.Path = @"C:\dumps\bios.bin";

        vm.SelectedName = "scph5500.bin";

        Assert.AreEqual(@"C:\dumps\bios.bin", vm.CardFile!.SourcePath);
        Assert.AreEqual("retroarch/system/scph5500.bin", vm.CardFile.CardPath);
        Assert.AreEqual("retroarch/system/scph5500.bin", vm.CardPath);
    }

    [TestMethod]
    public void Constructor_MultiName_ExposesTheChoiceWithThePreferredNameSelected()
    {
        var vm = new BiosFileViewModel(_dialogs, Psx);

        Assert.AreSame(Psx, vm.Bios);
        Assert.AreEqual("PlayStation BIOS", vm.Label);
        Assert.AreEqual("PlayStation BIOS", vm.Field.Label);
        CollectionAssert.AreEqual(Psx.Names.ToArray(), vm.Names.ToArray());
        Assert.IsTrue(vm.HasChoice);
        Assert.AreEqual("scph5501.bin", vm.SelectedName);
        Assert.AreEqual("retroarch/system/scph5501.bin", vm.CardPath);
        Assert.IsNull(vm.OnCardAs);
        Assert.IsFalse(vm.IsOnCard);
        Assert.IsTrue(vm.IsWanted);
        Assert.IsNull(vm.CardFile);
    }

    [TestMethod]
    public void Constructor_NullArguments_Throw()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new BiosFileViewModel(null!, Lynx));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BiosFileViewModel(_dialogs, null!));
    }

    [TestMethod]
    public void Constructor_SingleName_NoChoice()
    {
        var vm = new BiosFileViewModel(_dialogs, Lynx);

        Assert.IsFalse(vm.HasChoice);
        Assert.AreEqual("lynxboot.img", vm.Label);
        Assert.AreEqual("lynxboot.img", vm.SelectedName);
        Assert.AreEqual("retroarch/system/lynxboot.img", vm.CardPath);
    }

    [TestMethod]
    public void Field_PathCleared_KeepsTheSelectedName()
    {
        var vm = new BiosFileViewModel(_dialogs, Psx);
        vm.Field.Path = @"C:\dumps\scph1001.bin";

        vm.Field.Path = null;

        Assert.AreEqual("scph1001.bin", vm.SelectedName);
        Assert.IsTrue(vm.IsWanted);
        Assert.IsNull(vm.CardFile);
    }

    [TestMethod]
    public void Field_PathWithAnAcceptedName_SelectsItIgnoringCase()
    {
        var vm = new BiosFileViewModel(_dialogs, Psx);

        vm.Field.Path = @"C:\dumps\SCPH1001.BIN";

        Assert.AreEqual("scph1001.bin", vm.SelectedName);
    }

    [TestMethod]
    public void Field_PathWithAnUnrelatedName_SelectsThePreferredName()
    {
        var vm = new BiosFileViewModel(_dialogs, Psx);
        vm.SelectedName = "scph5500.bin";

        vm.Field.Path = @"C:\dumps\bios.bin";

        Assert.AreEqual("scph5501.bin", vm.SelectedName);
    }

    [TestMethod]
    public async Task Pick_Always_OffersEveryAcceptedNameAndAllFiles()
    {
        var vm = new BiosFileViewModel(_dialogs, Psx);
        _dialogs.NextPaths.Enqueue(@"C:\dumps\scph5500.bin");

        await vm.Field.PickCommand.ExecuteAsync(null);

        var (title, filters) = _dialogs.FilePicks.Single();
        Assert.AreEqual("PlayStation BIOS", title);
        Assert.AreEqual(2, filters.Length);
        Assert.AreEqual("PlayStation BIOS", filters[0].Name);
        CollectionAssert.AreEqual(Psx.Names.ToArray(), filters[0].Patterns);
        Assert.AreEqual("*.*", filters[1].Patterns.Single());
        Assert.AreEqual("scph5500.bin", vm.SelectedName);
    }

    [TestMethod]
    public void PropertyChanged_SelectedNameChanged_RaisesCardPathAndCardFile()
    {
        var vm = new BiosFileViewModel(_dialogs, Psx);
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.SelectedName = "scph1001.bin";

        CollectionAssert.Contains(raised, nameof(BiosFileViewModel.CardPath));
        CollectionAssert.Contains(raised, nameof(BiosFileViewModel.CardFile));
    }

    [TestMethod]
    public void Status_OnCard_NamesTheFileFound()
    {
        var vm = new BiosFileViewModel(_dialogs, Psx);

        vm.OnCardAs = "scph5500.bin";

        Assert.AreEqual("Already on the SD card (scph5500.bin).", vm.Status);
    }

    [TestMethod]
    public void Status_Picked_NamesTheCardPath()
    {
        var vm = new BiosFileViewModel(_dialogs, Psx);

        vm.Field.Path = @"C:\dumps\scph1001.bin";

        StringAssert.StartsWith(vm.Status, "Will be copied to SD:/retroarch/system/scph1001.bin");
    }

    [TestMethod]
    public void Status_Wanted_SaysNotOnTheCard()
    {
        var vm = new BiosFileViewModel(_dialogs, Lynx);

        StringAssert.StartsWith(vm.Status, "Not on the SD card.");
    }
}
