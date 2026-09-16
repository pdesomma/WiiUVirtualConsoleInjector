using System.IO.Compression;
using WiiUSharp;
using WiiUSharp.Nus;

namespace PD.WiiU.VirtualConsole.Infrastructure.Tests;

[TestClass]
public class UwuvciLegacyImportTests
{
    private const string Json = "[{\"titleId\":\"0005000010153100\",\"name\":\"Dr. Mario\",\"region\":\"UnitedStates\",\"console\":\"Nes\"},{\"titleId\":\"0005000010153200\",\"name\":\"Dr. Mario\",\"region\":\"Europe\",\"console\":\"Nes\"}]";
    private const string AppXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><app type=\"complex\" access=\"777\"><version type=\"unsignedInt\" length=\"4\">16</version><title_id type=\"hexBinary\" length=\"8\">0005000010153100</title_id><group_id type=\"hexBinary\" length=\"4\">00000000</group_id><title_version type=\"hexBinary\" length=\"2\">0000</title_version></app>";

    private string _app = null!;
    private JsonKeyStore _keys = null!;
    private string _root = null!;
    private string _settings = null!;
    private DirectoryBaseStore _store = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "UwuvciLegacyImportTests", Guid.NewGuid().ToString("N"));
        _app = Path.Combine(_root, "UWUVCI AIO");
        _settings = Path.Combine(_root, "UWUVCI-V3", "settings.json");
        _keys = new JsonKeyStore(Path.Combine(_root, "ours", "keys.json"));
        _store = new DirectoryBaseStore(Path.Combine(_root, "ours", "bases"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void Find_NothingThere_IsNull()
    {
        Assert.IsNull(Create().Find());
    }

    [TestMethod]
    public void Find_SettingsAndAppFolder_ReadsKeysBasesAndSettings()
    {
        WriteSettings("{\"BasePath\":\"C:\\\\gone\",\"OutPath\":\"" + _root.Replace("\\", "\\\\") + "\",\"Ckey\":\"00112233445566778899aabbccddeeff\",\"ndsw\":true,\"snesw\":false,\"gczw\":true}");
        WriteBase("Dr. Mario [US]", "0005000010153100");
        WriteBase("Not a base", null);
        WriteKeys("nes.vck");

        var install = Create().Find()!;

        Assert.AreEqual(_settings, install.Location);
        Assert.IsNotNull(install.CommonKey);
        Assert.AreEqual(_root, install.OutputFolder);
        CollectionAssert.AreEquivalent(new[] { InjectionWarning.NdsDsiEnhanced, InjectionWarning.GameCubeGcz }, install.SuppressedWarnings.ToArray());
        Assert.AreEqual(1, install.Bases.Count);
        Assert.AreEqual("Dr. Mario", install.Bases[0].Base.Name);
        Assert.AreEqual(Region.UnitedStates, install.Bases[0].Base.Region);
        Assert.AreEqual(2, install.TitleKeys.Count, "the settings' base folder is gone, so the one beside bin is used");
        Assert.IsTrue(install.HasAnything);
    }

    [TestMethod]
    public void Find_OnlyAppFolder_StillFindsBasesAndKeys()
    {
        WriteBase("Dr. Mario [US]", "0005000010153100");

        var install = Create().Find()!;

        Assert.AreEqual(_app, install.Location);
        Assert.IsNull(install.CommonKey);
        Assert.IsNull(install.OutputFolder);
        Assert.AreEqual(1, install.Bases.Count);
    }

    [TestMethod]
    public void Find_SettingsWithBadKeyAndNoData_IsNull()
    {
        WriteSettings("{\"Ckey\":\"zz\",\"OutPath\":\"C:\\\\nope\"}");

        Assert.IsNull(Create().Find());
    }

    [TestMethod]
    public async Task ImportAsync_TakesWhatIsMissingAndLeavesTheRest()
    {
        WriteSettings("{\"Ckey\":\"00112233445566778899aabbccddeeff\"}");
        WriteBase("Dr. Mario [US]", "0005000010153100");
        WriteKeys("nes.vck");
        _keys.SetTitleKey(TitleId.Parse("0005000010153200"), new EncryptedTitleKey(new byte[16]));
        var import = Create();
        var install = import.Find()!;
        var progress = new List<string>();

        var report = await import.ImportAsync(install, new Progress<string>(progress.Add));

        Assert.IsTrue(report.CommonKeyAdded);
        Assert.AreEqual(1, report.TitleKeysAdded, "the key already stored stays as it was");
        Assert.AreEqual(1, report.BasesAdded);
        Assert.AreEqual(0, report.Failures.Count);
        Assert.IsFalse(report.IsEmpty);
        Assert.AreEqual(CommonKey.Parse("00112233445566778899aabbccddeeff"), _keys.CommonKey);
        Assert.AreEqual(new EncryptedTitleKey(new byte[16]), _keys.GetTitleKey(TitleId.Parse("0005000010153200")));
        Assert.IsTrue(_store.Locate(install.Bases[0].Base).Exists);

        var again = await import.ImportAsync(install);
        Assert.IsTrue(again.IsEmpty);
    }

    [TestMethod]
    public async Task ImportAsync_BaseFolderBroken_ReportsIt()
    {
        var folder = WriteBase("Dr. Mario [US]", "0005000010153100");
        var import = Create();
        var install = import.Find()!;
        Directory.Delete(Path.Combine(folder, "content"), recursive: true);

        var report = await import.ImportAsync(install);

        Assert.AreEqual(0, report.BasesAdded);
        Assert.AreEqual(1, report.Failures.Count);
        StringAssert.StartsWith(report.Failures[0], "Dr. Mario");
    }

    [TestMethod]
    public void Constructor_NullArguments_Throw()
    {
        var catalog = BaseCatalog.Parse(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(Json)));
        Assert.ThrowsExactly<ArgumentNullException>(() => new UwuvciLegacyImport(null!, _keys, _store));
        Assert.ThrowsExactly<ArgumentNullException>(() => new UwuvciLegacyImport(catalog, null!, _store));
        Assert.ThrowsExactly<ArgumentNullException>(() => new UwuvciLegacyImport(catalog, _keys, null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => new UwuvciLegacyImport(catalog, _keys, _store, null!, Array.Empty<string>()));
        Assert.ThrowsExactly<ArgumentNullException>(() => new UwuvciLegacyImport(catalog, _keys, _store, _settings, null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => Create().ImportAsync(null!).GetAwaiter().GetResult());
        StringAssert.EndsWith(UwuvciLegacyImport.DefaultSettingsPath(), Path.Combine("UWUVCI-V3", "settings.json"));
    }

    private UwuvciLegacyImport Create() =>
        new(BaseCatalog.Parse(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(Json))), _keys, _store, _settings, new[] { _app });

    private string WriteBase(string name, string? titleId)
    {
        var folder = Path.Combine(_app, "bin", "BaseGames", name);
        Directory.CreateDirectory(Path.Combine(folder, "code"));
        Directory.CreateDirectory(Path.Combine(folder, "content"));
        Directory.CreateDirectory(Path.Combine(folder, "meta"));
        if (titleId is not null)
        {
            File.WriteAllText(Path.Combine(folder, "code", "app.xml"), AppXml.Replace("0005000010153100", titleId));
            File.WriteAllText(Path.Combine(folder, "meta", "meta.xml"), "<menu><title_id type=\"hexBinary\" length=\"8\">" + titleId + "</title_id></menu>");
            File.WriteAllBytes(Path.Combine(folder, "content", "data.bin"), new byte[] { 1 });
        }
        return folder;
    }

    private void WriteKeys(string name)
    {
        var folder = Path.Combine(_app, "bin", "keys");
        Directory.CreateDirectory(folder);
        var payload = Convert.FromBase64String(UwuvciKeyFileTests.SamplePayload);
        using var file = File.Create(Path.Combine(folder, name));
        using var gzip = new GZipStream(file, CompressionMode.Compress);
        gzip.Write(payload, 0, payload.Length);
    }

    private void WriteSettings(string json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settings)!);
        File.WriteAllText(_settings, json);
    }
}
