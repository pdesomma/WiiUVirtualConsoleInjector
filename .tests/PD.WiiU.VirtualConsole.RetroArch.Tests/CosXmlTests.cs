using System.Text;

namespace PD.WiiU.VirtualConsole.RetroArch.Tests;

[TestClass]
public class CosXmlTests
{
    private const string Minimal = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<app type=\"complex\" access=\"777\">\n  <version type=\"unsignedInt\" length=\"4\">1</version>\n  <argstr type=\"string\" length=\"4096\">core.rpx fs:/vol/content/a.md</argstr>\n  <!--kept-->\n  <max_size type=\"hexBinary\" length=\"4\">40000000</max_size>\n</app>";

    private string _root = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = TestPaths.TempRoot();
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void Arguments_DocumentWithoutArgstr_ThrowsInvalidDataException()
    {
        var cos = CosXml.Load(Write("<app><version>1</version></app>"));

        Assert.ThrowsExactly<InvalidDataException>(() => _ = cos.Arguments);
    }

    [TestMethod]
    public void Arguments_MinimalDocument_ReadsArgstr()
    {
        var cos = CosXml.Load(Write(Minimal));

        Assert.AreEqual("core.rpx fs:/vol/content/a.md", cos.Arguments);
    }

    [TestMethod]
    public void Arguments_SetNull_ThrowsArgumentNullException()
    {
        var cos = CosXml.Load(Write(Minimal));

        Assert.ThrowsExactly<ArgumentNullException>(() => cos.Arguments = null!);
    }

    [TestMethod]
    public void Load_RootIsNotApp_ThrowsInvalidDataException()
    {
        var path = Write("<menu><argstr>x</argstr></menu>");

        Assert.ThrowsExactly<InvalidDataException>(() => CosXml.Load(path));
    }

    [TestMethod]
    public void Save_AfterSettingArguments_RoundTrips()
    {
        var path = Write(Minimal);
        var cos = CosXml.Load(path);

        cos.Arguments = "picodrive_libretro.rpx fs:/vol/content/b.bin";
        cos.Save(path);

        Assert.AreEqual("picodrive_libretro.rpx fs:/vol/content/b.bin", CosXml.Load(path).Arguments);
    }

    [TestMethod]
    public void Save_Always_StartsWithDeclarationAndNoBom()
    {
        var path = Write(Minimal);
        var cos = CosXml.Load(path);

        cos.Save(path);

        var bytes = File.ReadAllBytes(path);
        Assert.IsTrue(bytes.Length > 5);
        Assert.AreNotEqual(0xEF, bytes[0], "no BOM");
        Assert.AreEqual("<?xml", Encoding.ASCII.GetString(bytes, 0, 5));
    }

    [TestMethod]
    public void Save_UntouchedElements_KeepFormatting()
    {
        var path = Write(Minimal);
        var cos = CosXml.Load(path);

        cos.Arguments = "x";
        cos.Save(path);

        var text = File.ReadAllText(path);
        StringAssert.Contains(text, "\n  <version type=\"unsignedInt\" length=\"4\">1</version>\n");
        StringAssert.Contains(text, "  <!--kept-->\n");
        StringAssert.Contains(text, "  <max_size type=\"hexBinary\" length=\"4\">40000000</max_size>\n</app>");
        StringAssert.Contains(text, "<argstr type=\"string\" length=\"4096\">x</argstr>");
    }

    private string Write(string xml)
    {
        var path = Path.Combine(_root, CosXml.FileName);
        File.WriteAllText(path, xml, new UTF8Encoding(false));
        return path;
    }
}
