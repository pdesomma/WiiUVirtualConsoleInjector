using PD.WiiU.VirtualConsole.Infrastructure;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Infrastructure.Tests;

[TestClass]
public class DirectoryBaseStoreTests
{
    private static readonly BaseTitle Base = new(new TitleId(TitleType.Game, 0x101C9300), "Test Base", Region.UnitedStates, SourceConsole.N64);

    private string _root = null!;

    [TestInitialize]
    public void Initialize() => _root = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Infrastructure.Tests", Guid.NewGuid().ToString("N"));

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void Constructor_BlankRoot_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new DirectoryBaseStore(""));
    }

    [TestMethod]
    public void Locate_KnownBase_UsesTitleIdFolder()
    {
        var store = new DirectoryBaseStore(_root);

        Assert.AreEqual(Path.Combine(_root, "00050000101C9300"), store.Locate(Base).Root);
    }

    [TestMethod]
    public void Locate_NullBase_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new DirectoryBaseStore(_root).Locate(null!));
    }

    [TestMethod]
    public async Task StageAsync_CompleteBase_CopiesEveryFileIncludingSubfolders()
    {
        var store = new DirectoryBaseStore(Path.Combine(_root, "bases"));
        var source = TitleDirectory.Create(store.Locate(Base).Root);
        File.WriteAllText(source.AppXmlPath, "<app/>");
        File.WriteAllBytes(Path.Combine(source.Content, "hif_000000.nfs"), new byte[] { 1, 2, 3 });
        Directory.CreateDirectory(Path.Combine(source.Meta, "nested"));
        File.WriteAllText(Path.Combine(source.Meta, "nested", "x.txt"), "x");

        var staged = await store.StageAsync(Base, Path.Combine(_root, "work"));

        Assert.AreEqual(Path.Combine(_root, "work"), staged.Root);
        Assert.AreEqual("<app/>", File.ReadAllText(staged.AppXmlPath));
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, File.ReadAllBytes(Path.Combine(staged.Content, "hif_000000.nfs")));
        Assert.AreEqual("x", File.ReadAllText(Path.Combine(staged.Meta, "nested", "x.txt")));
        Assert.IsTrue(File.Exists(source.AppXmlPath));
    }

    [TestMethod]
    public async Task StageAsync_IncompleteBase_ThrowsDirectoryNotFoundException()
    {
        var store = new DirectoryBaseStore(_root);
        Directory.CreateDirectory(Path.Combine(store.Locate(Base).Root, "code"));

        await Assert.ThrowsExactlyAsync<DirectoryNotFoundException>(() => store.StageAsync(Base, Path.Combine(_root, "work")));
    }

    [TestMethod]
    public async Task StageAsync_MissingBase_ThrowsDirectoryNotFoundException()
    {
        var store = new DirectoryBaseStore(_root);

        await Assert.ThrowsExactlyAsync<DirectoryNotFoundException>(() => store.StageAsync(Base, Path.Combine(_root, "work")));
    }
}
