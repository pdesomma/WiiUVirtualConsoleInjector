using PD.WiiU.VirtualConsole.Infrastructure;
using WiiUSharp;
using WiiUSharp.Nus;

namespace PD.WiiU.VirtualConsole.Infrastructure.Tests;

[TestClass]
public class JsonKeyStoreTests
{
    private const string CommonKeyHex = "0f1e2d3c4b5a69788796a5b4c3d2e1f0";
    private const string TitleKeyHex = "00112233445566778899aabbccddeeff";
    private static readonly TitleId TitleId = new(TitleType.Game, 0x101C9300);

    private string _root = null!;
    private string _path = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Infrastructure.Tests", Guid.NewGuid().ToString("N"));
        _path = Path.Combine(_root, "keys", "keys.json");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void CommonKey_MissingFile_IsNull()
    {
        Assert.IsNull(new JsonKeyStore(_path).CommonKey);
        Assert.IsFalse(File.Exists(_path));
    }

    [TestMethod]
    public void CommonKey_SetThenReloaded_RoundTrips()
    {
        new JsonKeyStore(_path).CommonKey = CommonKey.Parse(CommonKeyHex);

        Assert.AreEqual(CommonKey.Parse(CommonKeyHex), new JsonKeyStore(_path).CommonKey);
    }

    [TestMethod]
    public void CommonKey_SetToNull_ClearsIt()
    {
        var store = new JsonKeyStore(_path) { CommonKey = CommonKey.Parse(CommonKeyHex) };

        store.CommonKey = null;

        Assert.IsNull(new JsonKeyStore(_path).CommonKey);
    }

    [TestMethod]
    public void Constructor_BlankPath_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new JsonKeyStore(" "));
    }

    [TestMethod]
    public void Constructor_RelativePath_ResolvesFilePath()
    {
        Assert.AreEqual(Path.GetFullPath("keys.json"), new JsonKeyStore("keys.json").FilePath);
    }

    [TestMethod]
    public void GetTitleKey_MalformedJson_ThrowsInvalidDataException()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, "{ not json");

        Assert.ThrowsExactly<InvalidDataException>(() => new JsonKeyStore(_path).GetTitleKey(TitleId));
    }

    [TestMethod]
    public void GetTitleKey_MalformedKeyHex_ThrowsInvalidDataException()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, "{ \"commonKey\": null, \"titleKeys\": { \"00050000101C9300\": \"zz\" } }");

        Assert.ThrowsExactly<InvalidDataException>(() => new JsonKeyStore(_path).GetTitleKey(TitleId));
    }

    [TestMethod]
    public void GetTitleKey_MalformedTitleId_ThrowsInvalidDataException()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, "{ \"titleKeys\": { \"nope\": \"" + TitleKeyHex + "\" } }");

        Assert.ThrowsExactly<InvalidDataException>(() => new JsonKeyStore(_path).GetTitleKey(TitleId));
    }

    [TestMethod]
    public void GetTitleKey_NullJson_ThrowsInvalidDataException()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, "null");

        Assert.ThrowsExactly<InvalidDataException>(() => new JsonKeyStore(_path).GetTitleKey(TitleId));
    }

    [TestMethod]
    public void GetTitleKey_UnknownTitle_IsNull()
    {
        Assert.IsNull(new JsonKeyStore(_path).GetTitleKey(TitleId));
    }

    [TestMethod]
    public void SetTitleKey_Key_WritesLowercaseHexUnderUppercaseTitleId()
    {
        var store = new JsonKeyStore(_path);
        store.CommonKey = CommonKey.Parse(CommonKeyHex.ToUpperInvariant());

        store.SetTitleKey(TitleId, EncryptedTitleKey.Parse(TitleKeyHex.ToUpperInvariant()));

        var json = File.ReadAllText(_path);
        StringAssert.Contains(json, "\"commonKey\": \"" + CommonKeyHex + "\"");
        StringAssert.Contains(json, "\"00050000101C9300\": \"" + TitleKeyHex + "\"");
    }

    [TestMethod]
    public void SetTitleKey_KeyThenReloaded_RoundTrips()
    {
        new JsonKeyStore(_path).SetTitleKey(TitleId, EncryptedTitleKey.Parse(TitleKeyHex));

        Assert.AreEqual(EncryptedTitleKey.Parse(TitleKeyHex), new JsonKeyStore(_path).GetTitleKey(TitleId));
    }

    [TestMethod]
    public void SetTitleKey_Null_RemovesEntry()
    {
        var store = new JsonKeyStore(_path);
        store.SetTitleKey(TitleId, EncryptedTitleKey.Parse(TitleKeyHex));

        store.SetTitleKey(TitleId, null);

        Assert.IsNull(store.GetTitleKey(TitleId));
        Assert.IsNull(new JsonKeyStore(_path).GetTitleKey(TitleId));
        Assert.IsFalse(File.ReadAllText(_path).Contains(TitleKeyHex));
    }

    [TestMethod]
    public void SetTitleKey_SecondTitle_KeepsFirst()
    {
        var other = new TitleId(TitleType.Game, 0x10179A00);
        var store = new JsonKeyStore(_path);
        store.SetTitleKey(TitleId, EncryptedTitleKey.Parse(TitleKeyHex));

        store.SetTitleKey(other, EncryptedTitleKey.Parse(CommonKeyHex));

        var reloaded = new JsonKeyStore(_path);
        Assert.AreEqual(EncryptedTitleKey.Parse(TitleKeyHex), reloaded.GetTitleKey(TitleId));
        Assert.AreEqual(EncryptedTitleKey.Parse(CommonKeyHex), reloaded.GetTitleKey(other));
    }

    [TestMethod]
    public void WiiCommonKey_SetThenReloaded_RoundTrips()
    {
        var key = new WiiSharp.CommonKey(Enumerable.Range(1, 16).Select(i => (byte)(i * 5)).ToArray());
        new JsonKeyStore(_path).WiiCommonKey = key;

        var reloaded = new JsonKeyStore(_path);

        Assert.AreEqual(key, reloaded.WiiCommonKey);
        StringAssert.Contains(File.ReadAllText(_path), "\"wiiCommonKey\": \"" + KeyHex.Format(key.ToArray()) + "\"");
        reloaded.WiiCommonKey = null;
        Assert.IsNull(new JsonKeyStore(_path).WiiCommonKey);
    }
}
