namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class BaseInspectionTests
{
    private string _root = null!;

    [TestInitialize]
    public void Initialize() => _root = TestTitle.TempRoot();

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void Layout_PopulatedTitle_Passes()
    {
        var inspection = new BaseInspection(TestTitle.Populate(_root)).Layout();

        Assert.IsTrue(inspection.Passed);
        Assert.AreEqual(0, inspection.Issues.Count);
    }

    [TestMethod]
    public void Layout_MissingFolders_ReportsEachFolder()
    {
        var inspection = new BaseInspection(new TitleDirectory(_root)).Layout();

        CollectionAssert.AreEqual(new[] { "code", "content", "meta", "code/app.xml", "meta/meta.xml" }, inspection.Issues.Select(i => i.Path).ToArray());
        Assert.IsTrue(inspection.Issues.All(i => i.Message.EndsWith("missing", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void Layout_BrokenMetaXml_ReportsParseFailure()
    {
        var title = TestTitle.Populate(_root);
        File.WriteAllText(title.MetaXmlPath, "<menu>");

        var inspection = new BaseInspection(title).Layout();

        Assert.AreEqual(1, inspection.Issues.Count);
        Assert.AreEqual("meta/meta.xml", inspection.Issues[0].Path);
        Assert.AreEqual("meta/meta.xml: " + inspection.Issues[0].Message, inspection.Issues[0].ToString());
    }

    [TestMethod]
    public void RequireFile_ChecksPresenceAndLength()
    {
        var title = TestTitle.Populate(_root);
        var inspection = new BaseInspection(title);

        Assert.IsTrue(inspection.RequireFile("content/data.bin", 3));
        Assert.IsFalse(inspection.RequireFile("content/data.bin", 4));
        Assert.IsFalse(inspection.RequireFile("content/nope.bin"));

        CollectionAssert.AreEqual(new[] { "content/data.bin", "content/nope.bin" }, inspection.Issues.Select(i => i.Path).ToArray());
        Assert.AreEqual("3 bytes, expected at least 4", inspection.Issues[0].Message);
    }

    [TestMethod]
    public void RequireDirectoryAndAny_ReportEmptyAndMissing()
    {
        var title = TestTitle.Populate(_root);
        Directory.CreateDirectory(Path.Combine(title.Content, "empty"));
        var inspection = new BaseInspection(title);

        Assert.IsTrue(inspection.RequireDirectory("content"));
        Assert.IsFalse(inspection.RequireDirectory("content/empty", nonEmpty: true));
        Assert.IsFalse(inspection.RequireDirectory("content/gone"));
        Assert.AreEqual(Path.Combine(title.Content, "data.bin"), inspection.RequireAny("content", "*.bin"));
        Assert.IsNull(inspection.RequireAny("content", "*.rpx"));
        Assert.IsNull(inspection.RequireAny("nowhere", "*"));

        CollectionAssert.AreEqual(new[] { "folder empty", "folder missing", "no *.rpx file", "no * file" }, inspection.Issues.Select(i => i.Message).ToArray());
    }

    [TestMethod]
    public void Parse_ThrowingCheck_BecomesIssue()
    {
        var inspection = new BaseInspection(TestTitle.Populate(_root));

        Assert.IsFalse(inspection.Parse("content/data.bin", () => throw new InvalidDataException("bad magic")));
        Assert.IsTrue(inspection.Parse("content/data.bin", () => { }));
        Assert.ThrowsExactly<ArgumentNullException>(() => inspection.Parse("x", null!));

        Assert.AreEqual("bad magic", inspection.Issues.Single().Message);
    }

    [TestMethod]
    public void Relative_StripsRootAndUsesForwardSlashes()
    {
        var title = TestTitle.Populate(_root);
        var inspection = new BaseInspection(title);

        Assert.AreEqual("code/app.xml", inspection.Relative(title.AppXmlPath));
        Assert.AreEqual(@"C:\elsewhere\x", inspection.Relative(@"C:\elsewhere\x").Replace('/', '\\'));
        Assert.ThrowsExactly<ArgumentNullException>(() => inspection.Relative(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BaseInspection(null!));
    }
}
