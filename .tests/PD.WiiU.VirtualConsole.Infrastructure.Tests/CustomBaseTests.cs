using PD.WiiU.VirtualConsole.Infrastructure;
using WiiUSharp;
using WiiUSharp.Nus;

namespace PD.WiiU.VirtualConsole.Infrastructure.Tests;

[TestClass]
public class CustomBaseTests
{
    private const string AppXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><app type=\"complex\" access=\"777\"><title_id type=\"hexBinary\" length=\"8\">000500021ABCDE00</title_id><group_id type=\"hexBinary\" length=\"4\">00001ABC</group_id><title_version type=\"hexBinary\" length=\"2\">0000</title_version></app>";
    private const string MetaXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><menu type=\"complex\" access=\"777\"><title_id type=\"hexBinary\" length=\"8\">000500021ABCDE00</title_id><group_id type=\"hexBinary\" length=\"4\">00001ABC</group_id><product_code type=\"string\" length=\"32\">WUP-N-ABCD</product_code><company_code type=\"string\" length=\"8\">0001</company_code><title_version type=\"unsignedInt\" length=\"4\">0</title_version><region type=\"hexBinary\" length=\"4\">00000001</region><drc_use type=\"unsignedInt\" length=\"4\">0</drc_use><longname_en type=\"string\" length=\"512\">Custom\nKart</longname_en><shortname_en type=\"string\" length=\"256\">Kart</shortname_en></menu>";
    private static readonly CommonKey CommonKey = new(Enumerable.Range(1, 16).Select(i => (byte)(i * 3)).ToArray());
    private static readonly BaseTitle Base = new(new TitleId(TitleType.Demo, 0x1ABCDE00), "Custom Kart", Region.Japan, SourceConsole.Wii) { IsCustom = true };

    private string _root = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Infrastructure.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_root, recursive: true);

    [TestMethod]
    public void BaseFolder_Inspect_ReadsTitleFoldersAndPackages()
    {
        var title = Title();
        var package = Package(title);

        var asTitle = BaseFolder.Inspect(title.Root)!;
        var asPackage = BaseFolder.Inspect(package)!;

        Assert.AreEqual(BaseFolderKind.Title, asTitle.Kind);
        Assert.AreEqual(Base.TitleId, asTitle.TitleId);
        Assert.AreEqual("Custom Kart", asTitle.Name, "the newline in the long name becomes a space");
        Assert.AreEqual(Region.Japan, asTitle.Region);
        Assert.AreEqual(BaseFolderKind.Package, asPackage.Kind);
        Assert.AreEqual(Base.TitleId, asPackage.TitleId);
        Assert.IsNull(asPackage.Name);
        Assert.IsNull(BaseFolder.Inspect(_root), "neither");
        Assert.IsNull(BaseFolder.Inspect(Path.Combine(_root, "missing")));
        Assert.ThrowsExactly<ArgumentNullException>(() => BaseFolder.Inspect(null!));

        File.WriteAllText(title.MetaXmlPath, "not xml");
        var broken = BaseFolder.Inspect(title.Root)!;
        Assert.AreEqual(BaseFolderKind.Title, broken.Kind);
        Assert.AreEqual(Base.TitleId, broken.TitleId, "app.xml still gives the id");
        Assert.IsNull(broken.Name);
    }

    [TestMethod]
    public async Task DirectoryBaseStore_ImportAsync_CopiesATitleFolder()
    {
        var store = new DirectoryBaseStore(Path.Combine(_root, "store"));
        var title = Title();
        var messages = new List<string>();

        var imported = await store.ImportAsync(Base, title.Root, null, new SyncProgress(messages.Add));

        Assert.AreEqual(store.Locate(Base).Root, imported.Root);
        Assert.IsTrue(imported.Exists);
        CollectionAssert.AreEqual(File.ReadAllBytes(Path.Combine(title.Content, "sub", "data.bin")), File.ReadAllBytes(Path.Combine(imported.Content, "sub", "data.bin")));
        Assert.AreEqual(AppXml, File.ReadAllText(imported.AppXmlPath));
        CollectionAssert.AreEqual(new[] { "Copying title" }, messages);
        Assert.IsFalse(Directory.Exists(imported.Root + ".import"));

        File.WriteAllBytes(Path.Combine(imported.Content, "stale.bin"), new byte[1]);
        await store.ImportAsync(Base, title.Root, null);
        Assert.IsFalse(File.Exists(Path.Combine(imported.Content, "stale.bin")), "a re-import replaces the old files");
    }

    [TestMethod]
    public async Task DirectoryBaseStore_ImportAsync_UnpacksAPackageWithTheCommonKey()
    {
        var store = new DirectoryBaseStore(Path.Combine(_root, "store"));
        var package = Package(Title());
        var messages = new List<string>();

        var imported = await store.ImportAsync(Base, package, CommonKey, new SyncProgress(messages.Add));

        Assert.IsTrue(imported.Exists);
        Assert.AreEqual(AppXml, File.ReadAllText(imported.AppXmlPath));
        Assert.IsTrue(File.Exists(Path.Combine(imported.Content, "sub", "data.bin")));
        Assert.AreEqual("Unpacking package", messages[0]);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => store.ImportAsync(Base, package, null), "needs the key");
        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => store.ImportAsync(Base, package, new CommonKey(new byte[16])), "wrong key");
        Assert.IsFalse(Directory.Exists(store.Locate(Base).Root + ".import"), "a failed import leaves nothing behind");
        Assert.IsTrue(imported.Exists, "and keeps the earlier import");
    }

    [TestMethod]
    public async Task DirectoryBaseStore_ImportAsync_RejectsOtherFolders()
    {
        var store = new DirectoryBaseStore(Path.Combine(_root, "store"));

        await Assert.ThrowsExactlyAsync<InvalidDataException>(() => store.ImportAsync(Base, _root, CommonKey));
        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => store.ImportAsync(null!, _root, CommonKey));
        await Assert.ThrowsExactlyAsync<ArgumentException>(() => store.ImportAsync(Base, " ", CommonKey));
    }

    [TestMethod]
    public void JsonCustomBases_AddAllRemove_PersistAcrossInstances()
    {
        var path = Path.Combine(_root, "data", "custom-bases.json");
        var first = new JsonCustomBases(path);
        var other = new BaseTitle(new TitleId(TitleType.Game, 0x10199900), "Homebrew NES", Region.Europe, SourceConsole.Nes);

        first.Add(Base);
        first.Add(other);
        first.Add(new BaseTitle(Base.TitleId, "Renamed", Region.UnitedStates, SourceConsole.Wii));

        var second = new JsonCustomBases(path);
        var all = second.All();
        Assert.AreEqual(2, all.Count);
        Assert.AreEqual("Renamed", all.Single(t => t.TitleId == Base.TitleId).Name, "same id replaces");
        Assert.AreEqual(Region.UnitedStates, all.Single(t => t.TitleId == Base.TitleId).Region);
        Assert.IsTrue(all.All(t => t.IsCustom));
        Assert.AreEqual(SourceConsole.Nes, all.Single(t => t.TitleId == other.TitleId).Console);

        Assert.IsTrue(second.Remove(other.TitleId));
        Assert.IsFalse(second.Remove(other.TitleId));
        Assert.AreEqual(1, new JsonCustomBases(path).All().Count);

        File.WriteAllText(path, "{ not json");
        Assert.AreEqual(0, new JsonCustomBases(path).All().Count, "a damaged file reads as empty");
        Assert.ThrowsExactly<ArgumentException>(() => new JsonCustomBases(" "));
        Assert.ThrowsExactly<ArgumentNullException>(() => first.Add(null!));
    }

    private sealed class SyncProgress : IProgress<string>
    {
        private readonly Action<string> _handler;

        public SyncProgress(Action<string> handler)
        {
            _handler = handler;
        }

        public void Report(string value) => _handler(value);
    }

    private static string Package(TitleDirectory title)
    {
        var output = Path.Combine(Path.GetDirectoryName(title.Root)!, "package");
        new NusTitlePacker(CommonKey).PackAsync(title, output).GetAwaiter().GetResult();
        return output;
    }

    private TitleDirectory Title()
    {
        var title = TitleDirectory.Create(Path.Combine(_root, "title"));
        File.WriteAllText(title.AppXmlPath, AppXml);
        File.WriteAllText(title.MetaXmlPath, MetaXml);
        Directory.CreateDirectory(Path.Combine(title.Content, "sub"));
        File.WriteAllBytes(Path.Combine(title.Content, "sub", "data.bin"), Enumerable.Range(0, 300).Select(i => (byte)i).ToArray());
        File.WriteAllBytes(Path.Combine(title.Code, "game.rpx"), new byte[64]);
        return title;
    }
}
