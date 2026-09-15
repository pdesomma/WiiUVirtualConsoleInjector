using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Ports;
using WiiUVirtualConsoleInjector;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class SettingsServiceTests
{
    private static readonly AppPaths Paths = new(Path.Combine(Path.GetTempPath(), "WiiUVirtualConsoleInjector.Tests", "data"));

    [TestMethod]
    public void Paths_Unset_FallBackToDataFolder()
    {
        var service = new SettingsService(new MemorySettingsStore(), Paths);

        Assert.AreEqual(Paths.DefaultBasePath, service.BasePath);
        Assert.AreEqual(Paths.DefaultOutputPath, service.OutputPath);
        Assert.AreEqual(Paths.WorkFolder, service.WorkPath);
    }

    [TestMethod]
    public void Update_Change_SavesAndRaisesChanged()
    {
        var store = new MemorySettingsStore();
        var service = new SettingsService(store, Paths);
        var raised = 0;
        service.Changed += (_, _) => raised++;

        service.Update(s => s with { BasePath = @"C:\bases" });

        Assert.AreEqual(@"C:\bases", service.BasePath);
        Assert.AreEqual(@"C:\bases", store.Saved!.BasePath);
        Assert.AreEqual(1, raised);
        Assert.ThrowsExactly<ArgumentNullException>(() => service.Update(null!));
        Assert.ThrowsExactly<InvalidOperationException>(() => service.Update(_ => null!));
    }

    [TestMethod]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new SettingsService(null!, Paths));
        Assert.ThrowsExactly<ArgumentNullException>(() => new SettingsService(new MemorySettingsStore(), null!));
        Assert.ThrowsExactly<ArgumentException>(() => new AppPaths(" "));
    }

    internal sealed class MemorySettingsStore : ISettingsStore
    {
        public AppSettings? Saved { get; private set; }

        public AppSettings Load() => Saved ?? new AppSettings();

        public void Save(AppSettings settings) => Saved = settings;
    }
}
