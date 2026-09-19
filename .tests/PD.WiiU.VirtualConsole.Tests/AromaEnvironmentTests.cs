namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class AromaEnvironmentTests
{
    private string _root = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = TestTitle.TempRoot();
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_root, recursive: true);

    [TestMethod]
    public void Inspect_BlankRoot_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => AromaEnvironment.Inspect(" "));
        Assert.ThrowsExactly<ArgumentException>(() => AromaEnvironment.Inspect(null!));
    }

    [TestMethod]
    public void Inspect_EnvironmentWithoutTheModule_IsInstalledWithoutPatches()
    {
        Directory.CreateDirectory(Path.Combine(_root, "wiiu", "environments", "aroma", "modules", "setup"));

        var environment = AromaEnvironment.Inspect(_root);

        Assert.IsTrue(environment.IsInstalled);
        Assert.IsFalse(environment.HasSigPatches);
    }

    [TestMethod]
    public void Inspect_EnvironmentWithTheModule_HasBoth()
    {
        var setup = Path.Combine(_root, "wiiu", "environments", "aroma", "modules", "setup");
        Directory.CreateDirectory(setup);
        File.WriteAllBytes(Path.Combine(setup, "01_sigpatches.rpx"), new byte[] { 1 });

        var environment = AromaEnvironment.Inspect(_root);

        Assert.IsTrue(environment.IsInstalled);
        Assert.IsTrue(environment.HasSigPatches);
    }

    [TestMethod]
    public void Inspect_NoEnvironmentFolder_IsNeither()
    {
        Directory.CreateDirectory(Path.Combine(_root, "wiiu", "apps"));

        var environment = AromaEnvironment.Inspect(_root);

        Assert.IsFalse(environment.IsInstalled);
        Assert.IsFalse(environment.HasSigPatches);
    }
}
