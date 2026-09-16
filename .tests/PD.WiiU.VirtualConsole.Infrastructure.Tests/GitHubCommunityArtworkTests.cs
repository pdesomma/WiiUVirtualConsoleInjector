using System.Net;
using System.Net.Http;
using PD.WiiU.VirtualConsole.Infrastructure;

namespace PD.WiiU.VirtualConsole.Infrastructure.Tests;

[TestClass]
public class GitHubCommunityArtworkTests
{
    private const string Base = "https://repo.test/images/";

    private FakeRepository _repository = null!;
    private GitHubCommunityArtwork _artwork = null!;

    [TestInitialize]
    public void Initialize()
    {
        _repository = new FakeRepository();
        _artwork = new GitHubCommunityArtwork(new HttpClient(_repository), Base);
    }

    [TestMethod]
    public async Task FindAsync_SecondIdHasJpgIconAndTv_ReturnsThatFolderWithItsExtras()
    {
        _repository.Files["n64/NGEE/iconTex.jpg"] = new byte[] { 1 };
        _repository.Files["n64/NGEE/bootTvTex.jpg"] = new byte[] { 2 };
        _repository.Files["n64/NGEE/game.ini"] = new byte[] { 3 };

        var hit = await _artwork.FindAsync(new[] { "n64/NEGE", "n64/NGEE" });

        Assert.IsNotNull(hit);
        Assert.AreEqual("n64/NGEE", hit!.Id);
        Assert.AreEqual(Base + "n64/NGEE/iconTex.jpg", hit.Icon.ToString());
        Assert.AreEqual(Base + "n64/NGEE/bootTvTex.jpg", hit.BootTv.ToString());
        Assert.IsNull(hit.BootDrc);
        Assert.AreEqual(Base + "n64/NGEE/game.ini", hit.GameIni!.ToString());
        Assert.IsNull(hit.BootSound);
        Assert.IsTrue(_repository.Requests.All(r => r.Method == HttpMethod.Head), "lookups only ask for headers");
    }

    [TestMethod]
    public async Task FindAsync_IconWithoutTvOrNothingAtAll_ReturnsNull()
    {
        _repository.Files["snes/ABCD/iconTex.png"] = new byte[] { 1 };

        Assert.IsNull(await _artwork.FindAsync(new[] { "snes/ABCD" }), "a folder needs both images");
        Assert.IsNull(await _artwork.FindAsync(new[] { "snes/WXYZ" }));
        Assert.IsNull(await _artwork.FindAsync(Array.Empty<string>()));
    }

    [TestMethod]
    public async Task FindAsync_GamePadScreenAndBootSound_AreReportedButIniOnlyForN64()
    {
        foreach (var name in new[] { "iconTex.tga", "bootTvTex.tga", "bootDrcTex.tga", "BootSound.btsnd", "game.ini" })
            _repository.Files["gba/AGBE/" + name] = new byte[] { 9 };

        var hit = await _artwork.FindAsync(new[] { "gba/AGBE" });

        Assert.AreEqual(Base + "gba/AGBE/bootDrcTex.tga", hit!.BootDrc!.ToString());
        Assert.AreEqual(Base + "gba/AGBE/BootSound.btsnd", hit.BootSound!.ToString());
        Assert.IsNull(hit.GameIni, "only N64 games take an INI");
    }

    [TestMethod]
    public async Task DownloadAsync_WritesTheFileAndCreatesTheFolder()
    {
        _repository.Files["nes/HIMJ/iconTex.png"] = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var root = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Infrastructure.Tests", Guid.NewGuid().ToString("N"));
        var destination = Path.Combine(root, "a", "iconTex.png");
        try
        {
            await _artwork.DownloadAsync(new Uri(Base + "nes/HIMJ/iconTex.png"), destination);

            CollectionAssert.AreEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, File.ReadAllBytes(destination));
            await Assert.ThrowsExactlyAsync<HttpRequestException>(() => _artwork.DownloadAsync(new Uri(Base + "nes/HIMJ/missing.png"), destination));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public async Task Arguments_Invalid_Throw()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new GitHubCommunityArtwork(null!));
        Assert.AreEqual(GitHubCommunityArtwork.DefaultBaseUrl, new GitHubCommunityArtwork(new HttpClient(_repository)).BaseUrl);
        Assert.AreEqual("https://x.test/y/", new GitHubCommunityArtwork(new HttpClient(_repository), "https://x.test/y").BaseUrl, "a missing slash is added");
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _artwork.FindAsync(null!));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _artwork.DownloadAsync(null!, "x"));
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _artwork.DownloadAsync(new Uri(Base), " "));
    }

    /// <summary>
    /// Serves a dictionary of paths under <see cref="Base"/>; HEAD and GET alike, 404 otherwise.
    /// </summary>
    private sealed class FakeRepository : HttpMessageHandler
    {
        public Dictionary<string, byte[]> Files { get; } = new(StringComparer.Ordinal);
        public List<HttpRequestMessage> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            var url = request.RequestUri!.ToString();
            if (!url.StartsWith(Base, StringComparison.Ordinal) || !Files.TryGetValue(url.Substring(Base.Length), out var bytes))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            if (request.Method == HttpMethod.Get)
                response.Content = new ByteArrayContent(bytes);
            return Task.FromResult(response);
        }
    }
}
