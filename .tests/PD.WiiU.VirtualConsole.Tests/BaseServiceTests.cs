using WiiUSharp.Nus;

namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class BaseServiceTests
{
    private static readonly CommonKey Common = new(Enumerable.Range(1, 16).Select(i => (byte)i).ToArray());
    private static readonly EncryptedTitleKey Title = new(Enumerable.Range(1, 16).Select(i => (byte)(i * 3)).ToArray());

    private string _root = null!;
    private FakeBaseStore _store = null!;
    private FakeKeyStore _keys = null!;
    private FakeBaseDownloader _downloader = null!;
    private BaseService _service = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = TestTitle.TempRoot();
        _store = new FakeBaseStore { Root = _root };
        _keys = new FakeKeyStore();
        _downloader = new FakeBaseDownloader { Root = _root };
        _service = new BaseService(new BaseCatalog(new[] { TestTitle.Base() }), _store, _keys, _downloader);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void HasTitleKey_FollowsTheKeyStoreRegardlessOfPresence()
    {
        var @base = TestTitle.Base();

        Assert.IsFalse(_service.HasTitleKey(@base));
        _keys.SetTitleKey(@base.TitleId, Title);
        Assert.IsTrue(_service.HasTitleKey(@base));
        TestTitle.Populate(_store.Locate(@base).Root);
        Assert.IsTrue(_service.HasTitleKey(@base));
        _keys.SetTitleKey(@base.TitleId, null);
        Assert.IsFalse(_service.HasTitleKey(@base), "a present base without its key still shows no key");
        Assert.ThrowsExactly<ArgumentNullException>(() => _service.HasTitleKey(null!));
    }

    [TestMethod]
    public void Status_KeysArriveThenBase_WalksEveryState()
    {
        var @base = TestTitle.Base();

        Assert.AreEqual(BaseStatus.NeedsCommonKey, _service.Status(@base));
        _keys.CommonKey = Common;
        Assert.AreEqual(BaseStatus.NeedsTitleKey, _service.Status(@base));
        _keys.SetTitleKey(@base.TitleId, Title);
        Assert.AreEqual(BaseStatus.Downloadable, _service.Status(@base));
        TestTitle.Populate(_store.Locate(@base).Root);
        Assert.AreEqual(BaseStatus.Present, _service.Status(@base));
        _keys.CommonKey = null;
        Assert.AreEqual(BaseStatus.Present, _service.Status(@base), "a stored base needs no keys");
        Assert.ThrowsExactly<ArgumentNullException>(() => _service.Status(null!));
    }

    [TestMethod]
    public async Task DownloadAsync_KeysPresent_PassesThemToTheDownloader()
    {
        var @base = TestTitle.Base();
        _keys.CommonKey = Common;
        _keys.SetTitleKey(@base.TitleId, Title);
        var reports = new List<BaseDownloadProgress>();

        var title = await _service.DownloadAsync(@base, new SyncProgress(reports.Add));

        Assert.AreEqual(_store.Locate(@base).Root, title.Root);
        var call = _downloader.Calls.Single();
        Assert.AreSame(@base, call.Base);
        Assert.AreEqual(Title, call.TitleKey);
        Assert.AreEqual(Common, call.CommonKey);
        Assert.AreEqual(BaseDownloadPhase.Downloading, reports.Single().Phase);
    }

    [TestMethod]
    public async Task DownloadAsync_MissingKeys_ThrowsInvalidOperationException()
    {
        var @base = TestTitle.Base();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _service.DownloadAsync(@base));
        _keys.CommonKey = Common;
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _service.DownloadAsync(@base));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _service.DownloadAsync(null!));
        Assert.AreEqual(0, _downloader.Calls.Count);
    }

    [TestMethod]
    public void Available_Console_ComesFromTheCatalog()
    {
        Assert.AreEqual(1, _service.Available(SourceConsole.N64).Count);
        Assert.AreEqual(0, _service.Available(SourceConsole.Snes).Count);
    }

    [TestMethod]
    public void Constructor_NullDependencies_ThrowsArgumentNullException()
    {
        var catalog = new BaseCatalog(Array.Empty<BaseTitle>());

        Assert.ThrowsExactly<ArgumentNullException>(() => new BaseService(null!, _store, _keys, _downloader));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BaseService(catalog, null!, _keys, _downloader));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BaseService(catalog, _store, null!, _downloader));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BaseService(catalog, _store, _keys, null!));
    }

    private sealed class SyncProgress : IProgress<BaseDownloadProgress>
    {
        private readonly Action<BaseDownloadProgress> _report;

        public SyncProgress(Action<BaseDownloadProgress> report)
        {
            _report = report;
        }

        public void Report(BaseDownloadProgress value) => _report(value);
    }
}
