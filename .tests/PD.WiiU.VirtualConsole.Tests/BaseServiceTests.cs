using WiiUSharp;
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
    private FakeCustomBases _custom = null!;
    private BaseService _service = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = TestTitle.TempRoot();
        _store = new FakeBaseStore { Root = _root };
        _keys = new FakeKeyStore();
        _downloader = new FakeBaseDownloader { Root = _root };
        _custom = new FakeCustomBases();
        _service = new BaseService(new BaseCatalog(new[] { TestTitle.Base() }), _store, _keys, _downloader, _custom);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void Available_CustomBases_FollowTheCatalogAndServeGameCubeFromWii()
    {
        var wii = new BaseTitle(new TitleId(TitleType.Game, 0x10199900), "My Wii Base", Region.Europe, SourceConsole.Wii);
        var n64 = new BaseTitle(new TitleId(TitleType.Game, 0x10199901), "My N64 Base", Region.Japan, SourceConsole.N64);
        _service.AddCustom(wii);
        _service.AddCustom(n64);

        var forN64 = _service.Available(SourceConsole.N64);
        var forCube = _service.Available(SourceConsole.GameCube);

        Assert.AreEqual(2, forN64.Count);
        Assert.IsFalse(forN64[0].IsCustom, "catalog first");
        Assert.IsTrue(forN64[1].IsCustom);
        Assert.AreEqual("My N64 Base", forN64[1].Name);
        Assert.AreEqual(1, forCube.Count);
        Assert.AreEqual(SourceConsole.GameCube, forCube[0].Console, "retagged like the catalog's Wii bases");
        Assert.IsTrue(forCube[0].IsCustom);
        Assert.AreEqual(0, _service.Available(SourceConsole.Nes).Count);

        Assert.IsTrue(_service.RemoveCustom(n64.TitleId));
        Assert.IsFalse(_service.RemoveCustom(n64.TitleId));
        Assert.AreEqual(1, _service.Available(SourceConsole.N64).Count);
        Assert.ThrowsExactly<ArgumentNullException>(() => _service.AddCustom(null!));
    }

    [TestMethod]
    public void Available_CustomGbaBase_ServesGameBoyRetagged()
    {
        var gba = new BaseTitle(new TitleId(TitleType.Game, 0x10199902), "My GBA Base", Region.UnitedStates, SourceConsole.Gba);
        _service.AddCustom(gba);

        var forGameBoy = _service.Available(SourceConsole.GameBoy);

        Assert.AreEqual(1, forGameBoy.Count);
        Assert.AreEqual(gba.TitleId, forGameBoy[0].TitleId);
        Assert.AreEqual("My GBA Base", forGameBoy[0].Name);
        Assert.AreEqual(SourceConsole.GameBoy, forGameBoy[0].Console);
        Assert.IsTrue(forGameBoy[0].IsCustom);
        Assert.AreEqual(SourceConsole.Gba, _service.Available(SourceConsole.Gba).Single().Console);
    }

    [TestMethod]
    public void Available_CustomWithACatalogTitleId_YieldsToTheCatalog()
    {
        var clash = new BaseTitle(TestTitle.Base().TitleId, "Impostor", Region.Japan, SourceConsole.N64);
        _service.AddCustom(clash);

        var titles = _service.Available(SourceConsole.N64);

        Assert.AreEqual(1, titles.Count);
        Assert.IsFalse(titles[0].IsCustom);
    }

    [TestMethod]
    public async Task ImportAsync_HandsTheFolderAndCommonKeyToTheStore()
    {
        var @base = new BaseTitle(new TitleId(TitleType.Game, 0x10199900), "Imported", Region.Europe, SourceConsole.Wii) { IsCustom = true };
        _keys.CommonKey = Common;
        var messages = new List<string>();

        var title = await _service.ImportAsync(@base, Path.Combine(_root, "source"), new StringProgress(messages.Add));

        Assert.IsTrue(title.Exists);
        Assert.AreEqual(1, _store.Imports.Count);
        Assert.AreEqual(Common, _store.Imports[0].Key);
        CollectionAssert.AreEqual(new[] { "Imported" }, messages);
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => _service.ImportAsync(null!, "x"));
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => _service.ImportAsync(@base, " "));
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
    public void SizeOnDisk_StoredBase_SumsItsFiles()
    {
        var @base = TestTitle.Base();

        Assert.IsNull(_service.SizeOnDisk(@base));
        TestTitle.Populate(_store.Locate(@base).Root);
        var size = _service.SizeOnDisk(@base);

        Assert.IsNotNull(size);
        Assert.AreEqual(ByteSize.OfDirectory(_store.Locate(@base).Root), size);
        Assert.IsTrue(size!.Value.Bytes > 0);
        Assert.ThrowsExactly<ArgumentNullException>(() => _service.SizeOnDisk(null!));
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

        Assert.ThrowsExactly<ArgumentNullException>(() => new BaseService(null!, _store, _keys, _downloader, _custom));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BaseService(catalog, null!, _keys, _downloader, _custom));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BaseService(catalog, _store, null!, _downloader, _custom));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BaseService(catalog, _store, _keys, null!, _custom));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BaseService(catalog, _store, _keys, _downloader, null!));
    }

    private sealed class StringProgress : IProgress<string>
    {
        private readonly Action<string> _handler;

        public StringProgress(Action<string> handler)
        {
            _handler = handler;
        }

        public void Report(string value) => _handler(value);
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
