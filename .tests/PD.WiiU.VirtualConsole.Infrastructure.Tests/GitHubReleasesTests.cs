using System.Net;
using System.Net.Http;
using System.Text;

namespace PD.WiiU.VirtualConsole.Infrastructure.Tests;

[TestClass]
public class GitHubReleasesTests
{
    private FakeApi _api = null!;
    private GitHubReleases _releases = null!;

    [TestInitialize]
    public void Initialize()
    {
        _api = new FakeApi();
        _releases = new GitHubReleases(new HttpClient(_api), "owner/app", new Uri("https://api.test/"));
    }

    [TestMethod]
    public async Task LatestAsync_Release_ReadsVersionAndPage()
    {
        _api.Body = "{\"tag_name\":\"v1.2.3\",\"html_url\":\"https://github.com/owner/app/releases/tag/v1.2.3\",\"name\":\"1.2.3\"}";

        var release = await _releases.LatestAsync();

        Assert.AreEqual(new Version(1, 2, 3), release!.Version);
        Assert.AreEqual("https://github.com/owner/app/releases/tag/v1.2.3", release.Page.ToString());
        Assert.AreEqual("https://api.test/repos/owner/app/releases/latest", _api.Request!.RequestUri!.ToString());
        Assert.IsTrue(_api.Request.Headers.Contains("User-Agent"));
    }

    [TestMethod]
    public async Task LatestAsync_NoReleases_IsNull()
    {
        _api.Status = HttpStatusCode.NotFound;

        Assert.IsNull(await _releases.LatestAsync());
    }

    [TestMethod]
    public async Task LatestAsync_OddBodies_NullOrThrow()
    {
        _api.Body = "{\"tag_name\":\"nightly\"}";
        Assert.IsNull(await _releases.LatestAsync());

        _api.Body = "{\"tag_name\":\"2.0\"}";
        var release = await _releases.LatestAsync();
        Assert.AreEqual(new Version(2, 0), release!.Version);
        Assert.AreEqual("https://github.com/owner/app/releases/latest", release.Page.ToString());

        _api.Body = "[]";
        Assert.IsNull(await _releases.LatestAsync());

        _api.Status = HttpStatusCode.Forbidden;
        await Assert.ThrowsExactlyAsync<HttpRequestException>(() => _releases.LatestAsync());
    }

    [TestMethod]
    public void ParseTag_Variants_ReadOrNull()
    {
        Assert.AreEqual(new Version(1, 0, 0), GitHubReleases.ParseTag("v1.0.0"));
        Assert.AreEqual(new Version(1, 0), GitHubReleases.ParseTag(" V1.0 "));
        Assert.IsNull(GitHubReleases.ParseTag("v1.0.0-beta"));
        Assert.IsNull(GitHubReleases.ParseTag(""));
        Assert.IsNull(GitHubReleases.ParseTag(null));
    }

    [TestMethod]
    public void Constructor_BadArguments_Throw()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new GitHubReleases(null!));
        Assert.ThrowsExactly<ArgumentException>(() => new GitHubReleases(new HttpClient(_api), "nope"));
        Assert.AreEqual(GitHubReleases.DefaultRepository, new GitHubReleases(new HttpClient(_api)).Repository);
        Assert.AreEqual("https://api.github.com/repos/pdesomma/WiiUVirtualConsoleInjector/releases/latest", new GitHubReleases(new HttpClient(_api)).LatestUri.ToString());
    }

    private sealed class FakeApi : HttpMessageHandler
    {
        public string Body { get; set; } = "{}";
        public HttpRequestMessage? Request { get; private set; }
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent(Body, Encoding.UTF8, "application/json") });
        }
    }
}
