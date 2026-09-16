using PD.WiiU.VirtualConsole.Infrastructure;

namespace PD.WiiU.VirtualConsole.Infrastructure.Tests;

[TestClass]
public class JsonSettingsStoreTests
{
    private string _root = null!;
    private string _path = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Infrastructure.Tests", Guid.NewGuid().ToString("N"));
        _path = Path.Combine(_root, "app", "settings.json");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var settings = new JsonSettingsStore(_path).Load();

        Assert.IsNull(settings.BasePath);
        Assert.AreEqual(0, settings.SuppressedWarnings.Count);
        Assert.IsFalse(File.Exists(_path));
    }

    [TestMethod]
    public void Save_ThenLoad_RoundTripsEveryField()
    {
        var store = new JsonSettingsStore(_path);
        var settings = new AppSettings { BasePath = @"C:\bases", OutputPath = @"D:\out", WorkPath = @"E:\tmp", SdPath = @"F:\", CopyToSdCard = true, CheckForUpdates = false, LastUpdateCheck = new DateTimeOffset(2026, 9, 16, 1, 2, 3, TimeSpan.Zero) }
            .Suppress(InjectionWarning.GameCubeGcz)
            .Suppress(InjectionWarning.NdsDsiEnhanced);

        store.Save(settings);
        var loaded = new JsonSettingsStore(_path).Load();

        Assert.AreEqual(@"C:\bases", loaded.BasePath);
        Assert.AreEqual(@"D:\out", loaded.OutputPath);
        Assert.AreEqual(@"E:\tmp", loaded.WorkPath);
        Assert.AreEqual(@"F:\", loaded.SdPath);
        Assert.IsTrue(loaded.CopyToSdCard);
        Assert.IsFalse(loaded.CheckForUpdates);
        Assert.AreEqual(new DateTimeOffset(2026, 9, 16, 1, 2, 3, TimeSpan.Zero), loaded.LastUpdateCheck);
        CollectionAssert.AreEquivalent(new[] { InjectionWarning.GameCubeGcz, InjectionWarning.NdsDsiEnhanced }, loaded.SuppressedWarnings.ToArray());
        var json = File.ReadAllText(_path);
        StringAssert.Contains(json, "\"basePath\"");
        StringAssert.Contains(json, "\"GameCubeGcz\"");
        Assert.IsFalse(File.Exists(_path + ".tmp"));
    }

    [TestMethod]
    public void Save_Twice_ReplacesTheFile()
    {
        var store = new JsonSettingsStore(_path);
        store.Save(new AppSettings { BasePath = @"C:\one" }.Suppress(InjectionWarning.SnesCoProcessor));

        store.Save(new AppSettings { OutputPath = @"C:\two" });
        var loaded = store.Load();

        Assert.IsNull(loaded.BasePath);
        Assert.AreEqual(@"C:\two", loaded.OutputPath);
        Assert.AreEqual(0, loaded.SuppressedWarnings.Count);
    }

    [TestMethod]
    public void Load_MalformedOrPartialFile_ReturnsDefaultsOrFillsGaps()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

        File.WriteAllText(_path, "{ not json");
        Assert.IsNull(new JsonSettingsStore(_path).Load().BasePath);

        File.WriteAllText(_path, "null");
        Assert.AreEqual(0, new JsonSettingsStore(_path).Load().SuppressedWarnings.Count);

        File.WriteAllText(_path, "{\"basePath\":\"  \",\"suppressedWarnings\":[\"Nonsense\"]}");
        Assert.IsNull(new JsonSettingsStore(_path).Load().BasePath, "an unknown warning name reads as defaults");

        File.WriteAllText(_path, "{\"outputPath\":\"C:/out\"}");
        var partial = new JsonSettingsStore(_path).Load();
        Assert.AreEqual("C:/out", partial.OutputPath);
        Assert.IsNull(partial.BasePath);
        Assert.AreEqual(0, partial.SuppressedWarnings.Count);
        Assert.AreEqual(AppTheme.Light, partial.Theme);
        Assert.IsTrue(partial.CheckForUpdates);
        Assert.IsNull(partial.LastUpdateCheck);
    }

    [TestMethod]
    public void Constructor_And_Save_RejectBadArguments()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new JsonSettingsStore(" "));
        Assert.ThrowsExactly<ArgumentNullException>(() => new JsonSettingsStore(_path).Save(null!));
        Assert.AreEqual(Path.GetFullPath("settings.json"), new JsonSettingsStore("settings.json").FilePath);
    }
}
