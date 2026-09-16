using WiiUSharp.Nus;
using WiiUSharp.Wud;

namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class WudDiscBackupTests
{
    private static readonly CommonKey Common = new(new byte[16]);

    private readonly WudDiscBackup _backup = new();
    private string _root = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "WudDiscBackupTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void Accepts_DumpExtensions_OnlyThose()
    {
        Assert.IsTrue(_backup.Accepts(@"C:\dumps\game.wud"));
        Assert.IsTrue(_backup.Accepts(@"C:\dumps\game.WUX"));
        Assert.IsTrue(_backup.Accepts(@"C:\dumps\game_part1.wud"));
        Assert.IsFalse(_backup.Accepts(@"C:\dumps\game.iso"));
        Assert.IsFalse(_backup.Accepts(""));
        Assert.IsFalse(_backup.Accepts(null!));
    }

    [TestMethod]
    public async Task UnpackAsync_NoKeyBeside_ThrowsFileNotFound()
    {
        var image = Path.Combine(_root, "game.wud");
        File.WriteAllBytes(image, new byte[0x20000]);

        await Assert.ThrowsExactlyAsync<FileNotFoundException>(() => _backup.UnpackAsync(image, null, Common, _root));
    }

    [TestMethod]
    public async Task UnpackAsync_NotADump_ThrowsInvalidData()
    {
        var image = Path.Combine(_root, "game.wud");
        File.WriteAllBytes(image, new byte[0x20000]);

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => _backup.UnpackAsync(image, new DiscKey(new byte[16]), Common, _root));
    }

    [TestMethod]
    public async Task UnpackAsync_BadArguments_Throw()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _backup.UnpackAsync(" ", null, Common, _root));
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _backup.UnpackAsync(Path.Combine(_root, "x.wud"), null, Common, ""));
        await Assert.ThrowsExactlyAsync<FileNotFoundException>(() => _backup.UnpackAsync(Path.Combine(_root, "x.wud"), null, Common, _root));
    }
}
