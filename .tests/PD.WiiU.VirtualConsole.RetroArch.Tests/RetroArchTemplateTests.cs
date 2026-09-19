using WiiUSharp;

namespace PD.WiiU.VirtualConsole.RetroArch.Tests;

[TestClass]
public class RetroArchTemplateTests
{
    private string _root = null!;

    [TestInitialize]
    public void Initialize() => _root = TestPaths.TempRoot();

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void ContentMount_Always_IsVolContent()
    {
        Assert.AreEqual("fs:/vol/content/", RetroArchTemplate.ContentMount);
    }

    [TestMethod]
    public async Task WriteAsync_EmptyRoot_WritesEveryCodeAndMetaFile()
    {
        var title = new TitleDirectory(_root);

        await RetroArchTemplate.WriteAsync(title);

        Assert.IsTrue(title.Exists);
        foreach (var file in RetroArchTemplate.CodeFiles.Concat(RetroArchTemplate.MetaFiles))
        {
            var path = Path.Combine(title.Root, file.Replace('/', Path.DirectorySeparatorChar));
            Assert.IsTrue(File.Exists(path), file);
            Assert.IsTrue(new FileInfo(path).Length > 0, file + " is empty");
        }
    }

    [TestMethod]
    public async Task WriteAsync_EmptyRoot_XmlParsesWithWiiUSharp()
    {
        var title = new TitleDirectory(_root);

        await RetroArchTemplate.WriteAsync(title);

        Assert.IsNotNull(AppXml.Load(title.AppXmlPath));
        Assert.IsNotNull(MetaXml.Load(title.MetaXmlPath));
        Assert.AreEqual(string.Empty, CosXml.Load(Path.Combine(title.Code, CosXml.FileName)).Arguments);
    }

    [TestMethod]
    public async Task WriteAsync_Null_ThrowsArgumentNullException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => RetroArchTemplate.WriteAsync(null!));
    }

    [TestMethod]
    public async Task WriteAsync_PreCancelledToken_ThrowsOperationCanceledException()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => RetroArchTemplate.WriteAsync(new TitleDirectory(_root), cancellation.Token));
    }
}
