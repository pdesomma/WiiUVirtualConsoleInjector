using System.Net;
using System.Net.Http;

namespace PD.WiiU.VirtualConsole.Infrastructure.Tests;

[TestClass]
public class GitHubNintendontSourceTests
{
    private const string Base = "https://example.test/nintendont/";

    private FakeRepository _repository = null!;
    private string _root = null!;
    private GitHubNintendontSource _source = null!;

    [TestInitialize]
    public void Initialize()
    {
        _repository = new FakeRepository();
        _repository.Files["loader/loader.dol"] = new byte[] { 1, 2, 3 };
        _repository.Files["nintendont/meta.xml"] = new byte[] { 4 };
        _repository.Files["nintendont/icon.png"] = new byte[] { 5, 6 };
        _source = new GitHubNintendontSource(new HttpClient(_repository), Base);
        _root = Path.Combine(Path.GetTempPath(), "GitHubNintendontSourceTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public async Task InstallAsync_AllFilesServed_WritesAppFolderAndReportsEach()
    {
        var progress = new List<string>();

        await _source.InstallAsync(_root, new Progress<string>(progress.Add));
        await Task.Delay(50);

        var folder = Path.Combine(_root, "apps", "nintendont");
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, File.ReadAllBytes(Path.Combine(folder, "boot.dol")));
        CollectionAssert.AreEqual(new byte[] { 4 }, File.ReadAllBytes(Path.Combine(folder, "meta.xml")));
        CollectionAssert.AreEqual(new byte[] { 5, 6 }, File.ReadAllBytes(Path.Combine(folder, "icon.png")));
        Assert.IsFalse(Directory.Exists(folder + ".download"));
        CollectionAssert.AreEquivalent(new[] { "Downloading boot.dol", "Downloading meta.xml", "Downloading icon.png" }, progress);
    }

    [TestMethod]
    public async Task InstallAsync_ExistingInstall_IsReplaced()
    {
        var folder = Path.Combine(_root, "apps", "nintendont");
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, "boot.dol"), new byte[] { 9 });
        File.WriteAllText(Path.Combine(folder, "stale.txt"), "x");

        await _source.InstallAsync(_root);

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, File.ReadAllBytes(Path.Combine(folder, "boot.dol")));
        Assert.IsFalse(File.Exists(Path.Combine(folder, "stale.txt")));
    }

    [TestMethod]
    public async Task InstallAsync_FileMissing_ThrowsAndLeavesExistingInstall()
    {
        var folder = Path.Combine(_root, "apps", "nintendont");
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, "boot.dol"), new byte[] { 9 });
        _repository.Files.Remove("nintendont/icon.png");

        await Assert.ThrowsExactlyAsync<HttpRequestException>(() => _source.InstallAsync(_root));

        CollectionAssert.AreEqual(new byte[] { 9 }, File.ReadAllBytes(Path.Combine(folder, "boot.dol")));
        Assert.IsFalse(Directory.Exists(folder + ".download"));
    }

    [TestMethod]
    public void Constructor_BaseUrl_DefaultsToForkAndEndsWithSlash()
    {
        Assert.AreEqual(GitHubNintendontSource.DefaultBaseUrl, new GitHubNintendontSource(new HttpClient(_repository)).BaseUrl);
        Assert.AreEqual("https://x.test/y/", new GitHubNintendontSource(new HttpClient(_repository), "https://x.test/y").BaseUrl);
        Assert.ThrowsExactly<ArgumentNullException>(() => new GitHubNintendontSource(null!));
    }

    [TestMethod]
    public async Task InstallAsync_BlankRoot_Throws()
    {
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _source.InstallAsync(" "));
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _source.InstallAsync(null!));
    }

    private sealed class FakeRepository : HttpMessageHandler
    {
        public Dictionary<string, byte[]> Files { get; } = new(StringComparer.Ordinal);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            if (!url.StartsWith(Base, StringComparison.Ordinal) || !Files.TryGetValue(url.Substring(Base.Length), out var bytes))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) });
        }
    }
}
