using System.Net;
using System.Net.Http;
using PD.WiiU.VirtualConsole.Infrastructure;
using WiiUSharp;
using WiiUSharp.Nus;

namespace PD.WiiU.VirtualConsole.Infrastructure.Tests;

[TestClass]
public class NusBaseDownloaderTests
{
    private const string AppXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><app type=\"complex\" access=\"777\"><version type=\"unsignedInt\" length=\"4\">16</version><os_version type=\"hexBinary\" length=\"8\">000500101000400A</os_version><title_id type=\"hexBinary\" length=\"8\">000500021ABCDE00</title_id><title_version type=\"hexBinary\" length=\"2\">0011</title_version><sdk_version type=\"unsignedInt\" length=\"4\">21204</sdk_version><app_type type=\"hexBinary\" length=\"4\">80000000</app_type><group_id type=\"hexBinary\" length=\"4\">00001ABC</group_id></app>";
    private const string BaseUrl = "http://cdn.test/ccs/download/";

    private static readonly BaseTitle Base = new(new TitleId(TitleType.Demo, 0x1ABCDE00), "Test Base", Region.UnitedStates, SourceConsole.N64);
    private static readonly CommonKey CommonKey = new(Enumerable.Range(1, 16).Select(i => (byte)(i * 7)).ToArray());
    private static readonly IReadOnlyDictionary<string, byte[]> Files = new Dictionary<string, byte[]>
    {
        ["code/app.xml"] = System.Text.Encoding.UTF8.GetBytes(AppXml),
        ["code/cos.xml"] = Pattern(300, 1),
        ["code/fw.img"] = Pattern(0x9000, 2),
        ["code/game.rpx"] = Pattern(70000, 3),
        ["content/a.bin"] = Pattern(100, 4),
        ["content/sub/b.bin"] = Pattern(NusFormat.HashBlockDataSize + 50, 5),
        ["meta/iconTex.tga"] = Pattern(640, 6),
        ["meta/meta.xml"] = Pattern(200, 7),
    };
    private static readonly TitleKey TitleKey = new(Enumerable.Range(1, 16).Select(i => (byte)(i * 11)).ToArray());
    private static readonly CommonKey WrongKey = new(Enumerable.Range(1, 16).Select(i => (byte)(i * 13)).ToArray());

    private string _root = null!;
    private string _packages = null!;
    private DirectoryBaseStore _store = null!;
    private FakeCdn _cdn = null!;
    private HttpClient _client = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Infrastructure.Tests", Guid.NewGuid().ToString("N"));
        var title = Path.Combine(_root, "title");
        foreach (var pair in Files)
        {
            var path = Path.Combine(title, pair.Key.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, pair.Value);
        }
        var packed = Path.Combine(_root, "packed");
        new NusPacker(CommonKey).Pack(title, packed, TitleKey);
        _packages = Path.Combine(_root, "packages");
        _store = new DirectoryBaseStore(Path.Combine(_root, "bases"));
        _cdn = new FakeCdn(packed, Base.TitleId);
        _client = new HttpClient(_cdn);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _client.Dispose();
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void Constructor_BadArguments_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new NusBaseDownloader(null!, _client, _packages));
        Assert.ThrowsExactly<ArgumentNullException>(() => new NusBaseDownloader(_store, null!, _packages));
        Assert.ThrowsExactly<ArgumentException>(() => new NusBaseDownloader(_store, _client, " "));
        Assert.ThrowsExactly<ArgumentException>(() => new NusBaseDownloader(_store, _client, _packages, "http://cdn.test/no-slash"));
    }

    [TestMethod]
    public void Constructor_ValidArguments_ResolvesPackageDirectoryAndDefaultsBaseUrl()
    {
        var downloader = new NusBaseDownloader(_store, _client, _packages);

        Assert.AreEqual(Path.GetFullPath(_packages), downloader.PackageDirectory);
        Assert.AreEqual(NusDownloader.DefaultBaseUrl, downloader.BaseUrl);
        Assert.AreEqual(BaseUrl, Downloader().BaseUrl);
    }

    [TestMethod]
    public async Task DownloadAsync_Cancelled_ThrowsOperationCanceledException()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => Downloader().DownloadAsync(Base, Wrapped(CommonKey), CommonKey, cancellationToken: source.Token));

        Assert.IsFalse(Directory.Exists(_store.Locate(Base).Root));
    }

    [TestMethod]
    public async Task DownloadAsync_CorruptContent_LeavesNoBaseOrTempFolderAndKeepsPackage()
    {
        _cdn.CorruptFile = "00000001";
        var target = _store.Locate(Base);

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => Downloader().DownloadAsync(Base, Wrapped(CommonKey), CommonKey));

        Assert.IsFalse(Directory.Exists(target.Root));
        Assert.IsFalse(Directory.Exists(target.Root + NusBaseDownloader.TempSuffix));
        Assert.IsTrue(File.Exists(Path.Combine(_packages, Base.TitleId.ToString(), NusFormat.TmdFileName)));
    }

    [TestMethod]
    public async Task DownloadAsync_ExistingBaseAndStaleTemp_ReplacesBoth()
    {
        var target = _store.Locate(Base);
        Directory.CreateDirectory(target.Root);
        File.WriteAllText(Path.Combine(target.Root, "stale.txt"), "old");
        Directory.CreateDirectory(target.Root + NusBaseDownloader.TempSuffix);
        File.WriteAllText(Path.Combine(target.Root + NusBaseDownloader.TempSuffix, "stale.txt"), "old");

        await Downloader().DownloadAsync(Base, Wrapped(CommonKey), CommonKey);

        Assert.IsTrue(target.Exists);
        Assert.IsFalse(File.Exists(Path.Combine(target.Root, "stale.txt")));
        Assert.IsFalse(Directory.Exists(target.Root + NusBaseDownloader.TempSuffix));
    }

    [TestMethod]
    public async Task DownloadAsync_NullBase_ThrowsArgumentNullException()
    {
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => Downloader().DownloadAsync(null!, Wrapped(CommonKey), CommonKey));
    }

    [TestMethod]
    public async Task DownloadAsync_ValidKeys_ReportsDownloadingThenUnpacking()
    {
        var reports = new List<BaseDownloadProgress>();

        await Downloader().DownloadAsync(Base, Wrapped(CommonKey), CommonKey, new SyncProgress<BaseDownloadProgress>(reports.Add));

        var downloading = reports.Where(r => r.Phase == BaseDownloadPhase.Downloading).ToArray();
        var unpacking = reports.Where(r => r.Phase == BaseDownloadPhase.Unpacking).ToArray();
        Assert.AreEqual(BaseDownloadPhase.Downloading, reports[0].Phase);
        Assert.AreEqual(BaseDownloadPhase.Unpacking, reports[reports.Count - 1].Phase);
        Assert.AreEqual(NusFormat.TmdFileName, downloading[0].Item);
        Assert.IsTrue(downloading.All(r => r.ItemCount > 0));
        Assert.AreEqual(downloading[downloading.Length - 1].BytesTotal, downloading[downloading.Length - 1].BytesReceived);
        Assert.IsTrue(unpacking.Length >= 2);
        CollectionAssert.AreEqual(Enumerable.Range(1, unpacking.Length).ToArray(), unpacking.Select(r => r.ItemNumber).ToArray());
        Assert.IsTrue(unpacking.All(r => r.ItemCount == 0 && r.BytesReceived == 0 && r.BytesTotal is null));
        Assert.AreEqual(reports.Count, reports.TakeWhile(r => r.Phase == BaseDownloadPhase.Downloading).Count() + unpacking.Length);
    }

    [TestMethod]
    public async Task DownloadAsync_ValidKeys_UnpacksIntoStoreAndDeletesPackage()
    {
        var target = _store.Locate(Base);

        var result = await Downloader().DownloadAsync(Base, Wrapped(CommonKey), CommonKey);

        Assert.AreEqual(target.Root, result.Root);
        Assert.IsTrue(result.Exists);
        foreach (var pair in Files)
            CollectionAssert.AreEqual(pair.Value, File.ReadAllBytes(Path.Combine(result.Root, pair.Key.Replace('/', Path.DirectorySeparatorChar))), pair.Key);
        Assert.IsFalse(Directory.Exists(Path.Combine(_packages, Base.TitleId.ToString())));
        Assert.IsFalse(Directory.Exists(target.Root + NusBaseDownloader.TempSuffix));
        Assert.IsTrue(_cdn.Requests.Contains("tmd"));
        Assert.IsFalse(_cdn.Requests.Contains("cetk"));
    }

    [TestMethod]
    public async Task DownloadAsync_WrongCommonKey_ThrowsInvalidDataExceptionAndKeepsPackage()
    {
        var target = _store.Locate(Base);

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => Downloader().DownloadAsync(Base, Wrapped(CommonKey), WrongKey));

        Assert.IsFalse(Directory.Exists(target.Root));
        Assert.IsFalse(Directory.Exists(target.Root + NusBaseDownloader.TempSuffix));
        Assert.IsTrue(File.Exists(Path.Combine(_packages, Base.TitleId.ToString(), NusFormat.TmdFileName)));
        Assert.IsTrue(File.Exists(Path.Combine(_packages, Base.TitleId.ToString(), NusFormat.ContentFileName(0))));
    }

    private NusBaseDownloader Downloader() => new(_store, _client, _packages, BaseUrl);

    private static byte[] Pattern(int length, int seed)
    {
        var bytes = new byte[length];
        for (var i = 0; i < length; i++)
            bytes[i] = (byte)(seed * 31 + i * 7);
        return bytes;
    }

    private static EncryptedTitleKey Wrapped(CommonKey commonKey) => TitleKey.Encrypt(Base.TitleId, commonKey);

    /// <summary>
    /// Serves a packed folder the way the content server lays it out.
    /// </summary>
    private sealed class FakeCdn : HttpMessageHandler
    {
        private readonly string _package;
        private readonly string _prefix;

        public FakeCdn(string package, TitleId titleId)
        {
            _package = package;
            _prefix = BaseUrl + titleId.Value.ToString("x16") + "/";
        }

        public string? CorruptFile { get; set; }
        public List<string> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var url = request.RequestUri!.ToString();
            Assert.IsTrue(url.StartsWith(_prefix, StringComparison.Ordinal), url);
            var name = url.Substring(_prefix.Length);
            Requests.Add(name);

            var file = name switch
            {
                "tmd" => NusFormat.TmdFileName,
                "cetk" => NusFormat.TicketFileName,
                _ when name.EndsWith(".h3", StringComparison.Ordinal) => name,
                _ => name + ".app",
            };
            var path = Path.Combine(_package, file);
            if (!File.Exists(path))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));

            var bytes = File.ReadAllBytes(path);
            if (name == CorruptFile)
                bytes[bytes.Length / 2] ^= 0xFF;
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
            response.Content.Headers.ContentLength = bytes.Length;
            return Task.FromResult(response);
        }
    }

    private sealed class SyncProgress<T> : IProgress<T>
    {
        private readonly Action<T> _report;

        public SyncProgress(Action<T> report)
        {
            _report = report;
        }

        public void Report(T value) => _report(value);
    }
}
