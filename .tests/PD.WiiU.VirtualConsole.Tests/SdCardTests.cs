using PD.WiiU.VirtualConsole.Ports;

namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class SdCardTests
{
    private string _root = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "sd-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void Constructor_NullDrives_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new SdCard(null!));
    }

    [TestMethod]
    public void Detect_OnePreparedDrive_PicksIt()
    {
        var card = new SdCard(new FakeRemovableDrives(Drive("E:\\"), Drive("F:\\", prepared: true)));

        Assert.AreEqual("F:\\", card.Detect()!.RootPath);
    }

    [TestMethod]
    public void Detect_OnlyOneDriveAtAll_PicksItEvenUnprepared()
    {
        var card = new SdCard(new FakeRemovableDrives(Drive("E:\\")));

        Assert.AreEqual("E:\\", card.Detect()!.RootPath);
    }

    [TestMethod]
    public void Detect_NoneOrAmbiguous_ReturnsNull()
    {
        Assert.IsNull(new SdCard(new FakeRemovableDrives()).Detect());
        Assert.IsNull(new SdCard(new FakeRemovableDrives(Drive("E:\\"), Drive("F:\\"))).Detect(), "two plain drives");
        Assert.IsNull(new SdCard(new FakeRemovableDrives(Drive("E:\\", prepared: true), Drive("F:\\", prepared: true))).Detect(), "two prepared drives");
    }

    [TestMethod]
    public async Task CopyAsync_Title_LandsUnderInstallWithItsTreeIntact()
    {
        var card = new SdCard(new FakeRemovableDrives());
        var title = Path.Combine(_root, "source", "[WUP]Test");
        Directory.CreateDirectory(Path.Combine(title, "code"));
        File.WriteAllText(Path.Combine(title, "title.tmd"), "tmd");
        File.WriteAllText(Path.Combine(title, "code", "app.xml"), "app");
        var reported = new List<string>();

        var destination = await card.CopyAsync(title, _root, new Progress<string>(reported.Add));

        Assert.AreEqual(Path.Combine(_root, SdCard.InstallFolder, "[WUP]Test"), destination);
        Assert.AreEqual("tmd", File.ReadAllText(Path.Combine(destination, "title.tmd")));
        Assert.AreEqual("app", File.ReadAllText(Path.Combine(destination, "code", "app.xml")));
        CollectionAssert.AreEquivalent(new[] { "title.tmd", "app.xml" }, reported);
    }

    [TestMethod]
    public async Task CopyAsync_TitleAlreadyOnTheCard_OverwritesIt()
    {
        var card = new SdCard(new FakeRemovableDrives());
        var title = Path.Combine(_root, "source", "[WUP]Test");
        Directory.CreateDirectory(title);
        File.WriteAllText(Path.Combine(title, "title.tmd"), "new");
        Directory.CreateDirectory(Path.Combine(_root, SdCard.InstallFolder, "[WUP]Test"));
        File.WriteAllText(Path.Combine(_root, SdCard.InstallFolder, "[WUP]Test", "title.tmd"), "old");

        await card.CopyAsync(title, _root);

        Assert.AreEqual("new", File.ReadAllText(Path.Combine(_root, SdCard.InstallFolder, "[WUP]Test", "title.tmd")));
    }

    [TestMethod]
    public async Task CopyAsync_Cancelled_StopsCopying()
    {
        var card = new SdCard(new FakeRemovableDrives());
        var title = Path.Combine(_root, "source", "[WUP]Test");
        Directory.CreateDirectory(title);
        File.WriteAllText(Path.Combine(title, "title.tmd"), "tmd");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => card.CopyAsync(title, _root, null, cancellation.Token));
    }

    [TestMethod]
    public async Task CopyAsync_MissingTitleOrBlankArguments_Throw()
    {
        var card = new SdCard(new FakeRemovableDrives());

        await Assert.ThrowsExactlyAsync<DirectoryNotFoundException>(() => card.CopyAsync(Path.Combine(_root, "nope"), _root));
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => card.CopyAsync(" ", _root));
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => card.CopyAsync(_root, " "));
    }

    [TestMethod]
    public void Description_Drive_ShowsRootLabelAndFreeSpace()
    {
        var drive = new RemovableDrive("E:\\", "WIIU", 3221225472, 16106127360, true);

        StringAssert.Contains(drive.Description, "E:\\");
        StringAssert.Contains(drive.Description, "WIIU");
        StringAssert.Contains(drive.ToString(), "3 GB");
        StringAssert.Contains(drive.ToString(), "15 GB");
    }

    private static RemovableDrive Drive(string root, bool prepared = false) =>
        new(root, "CARD", 8589934592, 16106127360, prepared);

    private sealed class FakeRemovableDrives : IRemovableDrives
    {
        private readonly RemovableDrive[] _drives;

        public FakeRemovableDrives(params RemovableDrive[] drives) => _drives = drives;

        public IReadOnlyList<RemovableDrive> List() => _drives;
    }
}
