namespace PD.WiiU.VirtualConsole.Infrastructure.Tests;

[TestClass]
public class CaptionFontTests
{
    private string _root = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "captionfont-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void Locate_ChosenFileExists_ReturnsIt()
    {
        var chosen = Path.Combine(_root, "mine.otf");
        File.WriteAllBytes(chosen, new byte[] { 1 });

        Assert.AreEqual(chosen, CaptionFont.Locate(chosen, new[] { _root }));
    }

    [TestMethod]
    public void Locate_ChosenFileMissing_ReturnsNullRatherThanGuessing()
    {
        File.WriteAllBytes(Path.Combine(_root, CaptionFont.LegacyFileName), new byte[] { 1 });

        Assert.IsNull(CaptionFont.Locate(Path.Combine(_root, "gone.otf"), new[] { _root }));
    }

    [TestMethod]
    public void Locate_NothingChosen_TakesTheFirstFolderHoldingTheLegacyFile()
    {
        var second = Path.Combine(_root, "second");
        Directory.CreateDirectory(second);
        var legacy = Path.Combine(second, CaptionFont.LegacyFileName);
        File.WriteAllBytes(legacy, new byte[] { 1 });

        Assert.AreEqual(legacy, CaptionFont.Locate(null, new[] { _root, second }));
        Assert.AreEqual(legacy, CaptionFont.Locate("  ", new[] { _root, second }));
    }

    [TestMethod]
    public void Locate_NothingAnywhere_ReturnsNull()
    {
        Assert.IsNull(CaptionFont.Locate(null, new[] { _root }));
        Assert.ThrowsExactly<ArgumentNullException>(() => CaptionFont.Locate(null, null!));
    }

    [TestMethod]
    public void LegacyToolFolders_Always_EndInTheToolsFolder()
    {
        var folders = CaptionFont.LegacyToolFolders().ToArray();

        Assert.IsTrue(folders.Length >= 1);
        Assert.IsTrue(folders.All(f => f.EndsWith(Path.Combine("bin", "Tools"), StringComparison.Ordinal)));
    }
}
