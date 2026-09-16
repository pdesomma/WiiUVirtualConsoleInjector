namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class WupPackageTests
{
    private string _root = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "WupPackageTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void Inspect_CompletePackage_ReadsTmdAndFindsNothingMissing()
    {
        var folder = FakePackage.Write(Path.Combine(_root, "Toad [0005000010180600]"), 16);

        var package = WupPackage.Inspect(folder);

        Assert.AreEqual("Toad [0005000010180600]", package.Name);
        Assert.AreEqual(FakePackage.Id, package.TitleId);
        Assert.AreEqual(16, package.TitleVersion);
        Assert.AreEqual(2, package.ContentCount);
        Assert.AreEqual(0x40000, package.Size);
        Assert.IsTrue(package.IsComplete);
        Assert.AreEqual(0, package.Missing.Count);
        Assert.IsTrue(WupPackage.LooksLike(folder));
        StringAssert.Contains(package.ToString(), "v16");
    }

    [TestMethod]
    public void Inspect_FilesGoneOrShort_ListsEachByName()
    {
        var folder = FakePackage.Write(Path.Combine(_root, "p"));
        File.Delete(Path.Combine(folder, "title.tik"));
        File.Delete(Path.Combine(folder, "00000003.h3"));
        File.WriteAllBytes(Path.Combine(folder, "00000000.app"), new byte[0x8000]);
        File.Delete(Path.Combine(folder, "00000003.app"));

        var package = WupPackage.Inspect(folder);

        Assert.IsFalse(package.IsComplete);
        CollectionAssert.AreEqual(new[] { "title.tik", "00000000.app (wrong size)", "00000003.app", "00000003.h3" }, package.Missing.ToArray());
    }

    [TestMethod]
    public void Inspect_BadInput_Throws()
    {
        var empty = Path.Combine(_root, "empty");
        Directory.CreateDirectory(empty);
        var garbage = Path.Combine(_root, "garbage");
        Directory.CreateDirectory(garbage);
        File.WriteAllBytes(Path.Combine(garbage, "title.tmd"), new byte[10]);

        Assert.ThrowsExactly<ArgumentException>(() => WupPackage.Inspect(" "));
        Assert.ThrowsExactly<DirectoryNotFoundException>(() => WupPackage.Inspect(Path.Combine(_root, "nope")));
        Assert.ThrowsExactly<InvalidDataException>(() => WupPackage.Inspect(empty));
        Assert.ThrowsExactly<InvalidDataException>(() => WupPackage.Inspect(garbage));
        Assert.IsFalse(WupPackage.LooksLike(empty));
        Assert.IsFalse(WupPackage.LooksLike(""));
    }
}
