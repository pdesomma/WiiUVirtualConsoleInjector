using PD.WiiU.VirtualConsole;
using WiiUSharp;
using WiiUSharp.Nus;
using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class BackupsViewModelTests
{
    private FakeDialogService _dialogs = null!;
    private FakeDiscBackup _discs = null!;
    private FakeKeyStore _keys = null!;
    private string _root = null!;
    private FakeSdCard _sdCard = null!;
    private FakeSettingsService _settings = null!;

    [TestInitialize]
    public void Initialize()
    {
        InlineSynchronizationContext.Install();
        _dialogs = new FakeDialogService();
        _discs = new FakeDiscBackup();
        _keys = new FakeKeyStore();
        _sdCard = new FakeSdCard();
        _settings = new FakeSettingsService();
        _root = Path.Combine(Path.GetTempPath(), "BackupsViewModelTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _settings.DetectedSdPath = Path.Combine(_root, "card");
        Directory.CreateDirectory(_settings.DetectedSdPath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new BackupsViewModel(null!, _settings, _sdCard, _keys, _discs));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BackupsViewModel(_dialogs, null!, _sdCard, _keys, _discs));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BackupsViewModel(_dialogs, _settings, null!, _keys, _discs));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BackupsViewModel(_dialogs, _settings, _sdCard, null!, _discs));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BackupsViewModel(_dialogs, _settings, _sdCard, _keys, null!));
    }

    [TestMethod]
    public async Task ActivateAsync_CardWithPackages_ListsThem()
    {
        var install = Path.Combine(_settings.DetectedSdPath, "install");
        WritePackage(Path.Combine(install, "Toad [0005000010180600]"));
        Directory.CreateDirectory(Path.Combine(install, "not a package"));
        var vm = Create();

        await vm.ActivateAsync();

        Assert.AreEqual("Backups", vm.Title);
        Assert.IsTrue(vm.HasCard);
        Assert.AreEqual(install, vm.CardStatus);
        Assert.AreEqual(1, vm.OnCard.Count);
        Assert.AreEqual("Toad [0005000010180600]", vm.OnCard[0].Name);
        Assert.AreEqual("Nothing picked.", vm.SourceStatus);
        Assert.IsFalse(vm.PushCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task ActivateAsync_NoCard_SaysSo()
    {
        _settings.DetectedSdPath = "";
        var vm = Create();

        await vm.ActivateAsync();

        Assert.IsFalse(vm.HasCard);
        StringAssert.Contains(vm.CardStatus, "No SD card");
        Assert.AreEqual(0, vm.OnCard.Count);
    }

    [TestMethod]
    public async Task PickPackage_Complete_ShowsSummaryAndCanPush()
    {
        _dialogs.FolderToPick = WritePackage(Path.Combine(_root, "Toad [0005000010180600]"));
        var vm = Create();

        await vm.PickPackageCommand.ExecuteAsync(null);

        Assert.IsTrue(vm.HasPackage);
        Assert.IsFalse(vm.HasImage);
        StringAssert.Contains(vm.SourceStatus, "0005000010180600 v0, 2 contents");
        Assert.IsTrue(vm.PushCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task PickPackage_Incomplete_NamesMissingAndCannotPush()
    {
        var folder = WritePackage(Path.Combine(_root, "p"));
        File.Delete(Path.Combine(folder, "title.tik"));
        _dialogs.FolderToPick = folder;
        var vm = Create();

        await vm.PickPackageCommand.ExecuteAsync(null);

        StringAssert.Contains(vm.SourceStatus, "missing title.tik");
        Assert.IsFalse(vm.PushCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task PickPackage_NotAPackage_ShowsError()
    {
        _dialogs.FolderToPick = _root;
        var vm = Create();

        await vm.PickPackageCommand.ExecuteAsync(null);

        Assert.IsFalse(vm.HasPackage);
        Assert.AreEqual(1, _dialogs.Errors.Count);
    }

    [TestMethod]
    public async Task Push_Package_CopiesToCardAndLogs()
    {
        var folder = WritePackage(Path.Combine(_root, "Toad [0005000010180600]"));
        _dialogs.FolderToPick = folder;
        _sdCard.CopyResult = Path.Combine(_settings.DetectedSdPath, "install", "Toad [0005000010180600]");
        var vm = Create();
        await vm.PickPackageCommand.ExecuteAsync(null);

        await vm.PushCommand.ExecuteAsync(null);

        CollectionAssert.AreEqual(new[] { (folder, _settings.DetectedSdPath) }, _sdCard.Copies);
        StringAssert.Contains(vm.Log[0], "Copied to");
        StringAssert.Contains(vm.Log[^1], "WUP Installer");
        Assert.IsFalse(vm.IsRunning);
        Assert.IsNull(vm.Progress);
    }

    [TestMethod]
    public async Task Push_CopyFails_ShowsErrorAndLogs()
    {
        _dialogs.FolderToPick = WritePackage(Path.Combine(_root, "p"));
        _sdCard.Failure = new IOException("full");
        var vm = Create();
        await vm.PickPackageCommand.ExecuteAsync(null);

        await vm.PushCommand.ExecuteAsync(null);

        StringAssert.Contains(vm.Log[^1], "full");
        Assert.AreEqual(1, _dialogs.Errors.Count);
        Assert.IsFalse(vm.IsRunning);
    }

    [TestMethod]
    public async Task PickImage_DumpWithoutKeyBeside_AsksForDiscKey()
    {
        var image = Path.Combine(_root, "game.wux");
        File.WriteAllBytes(image, new byte[1]);
        _dialogs.FileToPick = image;
        var vm = Create();

        await vm.PickImageCommand.ExecuteAsync(null);

        Assert.IsTrue(vm.HasImage);
        Assert.IsTrue(vm.NeedsDiscKey);
        StringAssert.Contains(vm.SourceStatus, "game.wux");
        StringAssert.Contains(vm.SourceStatus, "disc key");
        Assert.IsTrue(vm.PushCommand.CanExecute(null));
    }

    [TestMethod]
    public async Task PickImage_NotADump_ShowsError()
    {
        _dialogs.FileToPick = Path.Combine(_root, "game.iso");
        var vm = Create();

        await vm.PickImageCommand.ExecuteAsync(null);

        Assert.IsFalse(vm.HasImage);
        Assert.AreEqual(1, _dialogs.Errors.Count);
    }

    [TestMethod]
    public async Task Push_Image_UnpacksThenCopiesEachPackage()
    {
        var image = Path.Combine(_root, "game.wud");
        File.WriteAllBytes(image, new byte[1]);
        File.WriteAllBytes(Path.Combine(_root, "game.key"), new byte[16]);
        _keys.CommonKey = new CommonKey(new byte[16]);
        _discs.Folders.Add(Path.Combine(_root, "out", "WUP-P-AKBE [0005000010180600]"));
        _dialogs.FileToPick = image;
        var vm = Create();
        await vm.PickImageCommand.ExecuteAsync(null);
        Assert.IsFalse(vm.NeedsDiscKey);

        await vm.PushCommand.ExecuteAsync(null);

        Assert.AreEqual(1, _discs.Unpacked.Count);
        Assert.AreEqual(image, _discs.Unpacked[0].Image);
        Assert.IsNull(_discs.Unpacked[0].DiscKey);
        Assert.AreEqual(_settings.OutputPath, _discs.Unpacked[0].Output);
        CollectionAssert.AreEqual(new[] { (_discs.Folders[0], _settings.DetectedSdPath) }, _sdCard.Copies);
        Assert.IsTrue(vm.Log.Any(l => l.StartsWith("Unpacked to")));
    }

    [TestMethod]
    public async Task Push_Image_NoCommonKeyOrNoDiscKey_Refuses()
    {
        var image = Path.Combine(_root, "game.wud");
        File.WriteAllBytes(image, new byte[1]);
        _dialogs.FileToPick = image;
        var vm = Create();
        await vm.PickImageCommand.ExecuteAsync(null);

        await vm.PushCommand.ExecuteAsync(null);
        StringAssert.Contains(_dialogs.Errors[0].Message, "common key");

        _keys.CommonKey = new CommonKey(new byte[16]);
        await vm.PushCommand.ExecuteAsync(null);
        StringAssert.Contains(_dialogs.Errors[1].Message, "disc key");

        vm.DiscKeyHex = "00112233445566778899AABBCCDDEEFF";
        await vm.PushCommand.ExecuteAsync(null);
        Assert.AreEqual(2, _dialogs.Errors.Count);
        Assert.IsNotNull(_discs.Unpacked[0].DiscKey);
        Assert.AreEqual(0, _sdCard.Copies.Count, "nothing to copy when the fake wrote no folders");
    }

    private BackupsViewModel Create() => new(_dialogs, _settings, _sdCard, _keys, _discs);

    private static string WritePackage(string folder)
    {
        Directory.CreateDirectory(folder);
        var contents = new[]
        {
            new ContentRecord(0, ContentType.Content | ContentType.Encrypted, 0x100, new byte[20], 0),
            new ContentRecord(1, ContentType.Content | ContentType.Encrypted | ContentType.Hashed, 0x200, new byte[20], 3),
        };
        File.WriteAllBytes(Path.Combine(folder, "title.tmd"), Tmd.Build(new TitleInfo(TitleId.Parse("0005000010180600"), 0), contents));
        File.WriteAllBytes(Path.Combine(folder, "title.tik"), new byte[848]);
        File.WriteAllBytes(Path.Combine(folder, "title.cert"), new byte[2560]);
        File.WriteAllBytes(Path.Combine(folder, "00000000.app"), new byte[0x100]);
        File.WriteAllBytes(Path.Combine(folder, "00000003.app"), new byte[0x200]);
        File.WriteAllBytes(Path.Combine(folder, "00000003.h3"), new byte[20]);
        return folder;
    }
}
