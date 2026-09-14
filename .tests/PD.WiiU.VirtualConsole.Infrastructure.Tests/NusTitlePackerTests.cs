using PD.WiiU.VirtualConsole.Infrastructure;
using WiiUSharp.Nus;

namespace PD.WiiU.VirtualConsole.Infrastructure.Tests;

[TestClass]
public class NusTitlePackerTests
{
    private const string AppXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><app type=\"complex\" access=\"777\"><title_id type=\"hexBinary\" length=\"8\">000500021ABCDE00</title_id><group_id type=\"hexBinary\" length=\"4\">00001ABC</group_id><title_version type=\"hexBinary\" length=\"2\">0000</title_version></app>";
    private static readonly CommonKey CommonKey = new(Enumerable.Range(1, 16).Select(i => (byte)(i * 3)).ToArray());

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
    public void Constructor_NoTitleKey_UsesDefault()
    {
        Assert.AreEqual(TitleKey.Parse(NusTitlePacker.DefaultTitleKey), new NusTitlePacker(CommonKey).TitleKey);
    }

    [TestMethod]
    public async Task PackAsync_StagedTitle_WritesTmdTicketCertAndContents()
    {
        var title = TitleDirectory.Create(Path.Combine(_root, "title"));
        File.WriteAllText(title.AppXmlPath, AppXml);
        File.WriteAllBytes(Path.Combine(title.Content, "hif_000000.nfs"), new byte[100]);
        File.WriteAllBytes(Path.Combine(title.Meta, "meta.xml"), new byte[20]);
        var output = Path.Combine(_root, "out");
        var messages = new List<string>();

        await new NusTitlePacker(CommonKey).PackAsync(title, output, new SyncProgress(messages.Add));

        var files = Directory.GetFiles(output).Select(f => Path.GetFileName(f)).OrderBy(f => f, StringComparer.Ordinal).ToArray();
        CollectionAssert.AreEqual(new[] { "00000000.app", "00000001.app", "00000002.app", "00000002.h3", "00000003.app", "00000003.h3", "title.cert", "title.tik", "title.tmd" }, files);
        Assert.AreEqual(4, messages.Count);
    }

    [TestMethod]
    public async Task PackAsync_NullTitle_ThrowsArgumentNullException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => new NusTitlePacker(CommonKey).PackAsync(null!, _root));
    }

    private sealed class SyncProgress : IProgress<string>
    {
        private readonly Action<string> _report;

        public SyncProgress(Action<string> report)
        {
            _report = report;
        }

        public void Report(string value) => _report(value);
    }
}
