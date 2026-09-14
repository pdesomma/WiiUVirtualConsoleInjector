namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class TitleDirectoryTests
{
    [TestMethod]
    public void Constructor_BlankRoot_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new TitleDirectory(" "));
    }

    [TestMethod]
    public void Constructor_ValidRoot_ExposesSubfolderPaths()
    {
        var root = TestTitle.TempRoot();

        var title = new TitleDirectory(root);

        Assert.AreEqual(Path.Combine(root, "code"), title.Code);
        Assert.AreEqual(Path.Combine(root, "content"), title.Content);
        Assert.AreEqual(Path.Combine(root, "meta"), title.Meta);
        Assert.AreEqual(Path.Combine(root, "code", "app.xml"), title.AppXmlPath);
        Assert.AreEqual(Path.Combine(root, "meta", "meta.xml"), title.MetaXmlPath);
        Assert.IsFalse(title.Exists);
    }

    [TestMethod]
    public void Create_NewRoot_MakesAllThreeSubfolders()
    {
        var root = TestTitle.TempRoot();
        try
        {
            var title = TitleDirectory.Create(root);

            Assert.IsTrue(title.Exists);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void Exists_MissingSubfolder_ReturnsFalse()
    {
        var root = TestTitle.TempRoot();
        try
        {
            var title = TitleDirectory.Create(root);
            Directory.Delete(title.Content);

            Assert.IsFalse(title.Exists);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
