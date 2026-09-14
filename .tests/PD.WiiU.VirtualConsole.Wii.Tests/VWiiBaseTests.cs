using System.Text;

namespace PD.WiiU.VirtualConsole.Wii.Tests;

[TestClass]
public class VWiiBaseTests
{
    private string _root = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Wii.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_root, recursive: true);

    [TestMethod]
    public void Inspect_CompleteBase_Passes()
    {
        var title = Stage(keyLength: 16, revision: "r590", nfs: true);

        Assert.AreEqual(0, VWiiBase.Inspect(title).Count);
        Assert.AreEqual(0, new WiiRomInjector(FakeDisc.CommonKey).Inspect(title).Count);
        Assert.AreEqual(0, new GameCubeRomInjector().Inspect(title).Count);
    }

    [TestMethod]
    public void Inspect_WrongKeyOldFirmwareNoNfs_ListsEach()
    {
        var title = Stage(keyLength: 17, revision: "r533", nfs: false);

        CollectionAssert.AreEqual(
            new[] { "code/htk.bin: expected exactly 16 bytes", "code/fw.img: IOS revision r533, patches target r590", "content: no hif_*.nfs file" },
            VWiiBase.Inspect(title).Select(i => i.ToString()).ToArray());
    }

    [TestMethod]
    public void Inspect_EmptyTitle_ReportsMissingFiles()
    {
        var title = TitleDirectory.Create(Path.Combine(_root, "empty"));
        File.WriteAllBytes(Path.Combine(title.Code, WiiRomInjector.FirmwareFileName), new byte[0x100]);

        CollectionAssert.AreEqual(
            new[] { "code/htk.bin: file missing", "code/fw.img: IOS revision unknown, patches target r590", "content: no hif_*.nfs file" },
            VWiiBase.Inspect(title).Select(i => i.ToString()).ToArray());
        Assert.ThrowsExactly<ArgumentNullException>(() => VWiiBase.Inspect(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => new WiiRomInjector(FakeDisc.CommonKey).Inspect(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => new GameCubeRomInjector().Inspect(null!));
    }

    private TitleDirectory Stage(int keyLength, string revision, bool nfs)
    {
        var title = TitleDirectory.Create(Path.Combine(_root, Guid.NewGuid().ToString("N")));
        File.WriteAllBytes(Path.Combine(title.Code, WiiRomInjector.NfsKeyFileName), new byte[keyLength]);
        var firmware = new byte[0x400];
        Encoding.ASCII.GetBytes("svn-" + revision).CopyTo(firmware, 0x80);
        File.WriteAllBytes(Path.Combine(title.Code, WiiRomInjector.FirmwareFileName), firmware);
        if (nfs)
            File.WriteAllBytes(Path.Combine(title.Content, "hif_000000.nfs"), new byte[16]);
        return title;
    }
}
