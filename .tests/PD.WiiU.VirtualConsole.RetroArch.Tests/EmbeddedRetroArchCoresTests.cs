namespace PD.WiiU.VirtualConsole.RetroArch.Tests;

[TestClass]
public class EmbeddedRetroArchCoresTests
{
    private const int GenesisPlusGxLength = 6729787;

    private string _root = null!;

    private static RetroArchCore GenesisPlusGx => EmbeddedRetroArchCores.All.Single(core => core.Id == "genesis_plus_gx");

    [TestInitialize]
    public void Initialize() => _root = TestPaths.TempRoot();

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void All_Always_HasThreeGenesisCoresWithGenesisPlusGxRecommended()
    {
        var genesis = EmbeddedRetroArchCores.All.Where(core => core.Console == SourceConsole.Genesis).ToArray();

        Assert.AreEqual(3, genesis.Length);
        CollectionAssert.AreEquivalent(new[] { "genesis_plus_gx", "genesis_plus_gx_wide", "picodrive" }, genesis.Select(core => core.Id).ToArray());
        Assert.AreEqual("genesis_plus_gx", EmbeddedRetroArchCores.All.Single(core => core.IsRecommended).Id);
    }

    [TestMethod]
    public void All_EveryCore_HasItsResourceCompiledIn()
    {
        var names = typeof(EmbeddedRetroArchCores).Assembly.GetManifestResourceNames();

        foreach (var core in EmbeddedRetroArchCores.All)
            CollectionAssert.Contains(names, EmbeddedRetroArchCores.ResourceName(core), core.Id);
    }

    [TestMethod]
    public void Available_Genesis_ListsRecommendedFirst()
    {
        var cores = new EmbeddedRetroArchCores().Available(SourceConsole.Genesis);

        Assert.AreEqual(3, cores.Count);
        Assert.AreEqual("genesis_plus_gx", cores[0].Id);
        Assert.IsTrue(cores[0].IsRecommended);
    }

    [TestMethod]
    public void Available_Nes_IsEmpty()
    {
        Assert.AreEqual(0, new EmbeddedRetroArchCores().Available(SourceConsole.Nes).Count);
    }

    [TestMethod]
    public void ResourceName_GenesisPlusGx_IsCoresGenesisPath()
    {
        Assert.AreEqual("cores/genesis/genesis_plus_gx_libretro.rpx", EmbeddedRetroArchCores.ResourceName(GenesisPlusGx));
    }

    [TestMethod]
    public void ResourceName_Null_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => EmbeddedRetroArchCores.ResourceName(null!));
    }

    [TestMethod]
    public async Task StageAsync_BlankDestination_ThrowsArgumentException()
    {
        var cores = new EmbeddedRetroArchCores();

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => cores.StageAsync(GenesisPlusGx, " "));
    }

    [TestMethod]
    public async Task StageAsync_CoreNotBundled_ThrowsArgumentException()
    {
        var cores = new EmbeddedRetroArchCores();
        var nestopia = new RetroArchCore("nestopia", "Nestopia", SourceConsole.Nes, "x");

        await Assert.ThrowsExactlyAsync<ArgumentException>(() => cores.StageAsync(nestopia, _root));
    }

    [TestMethod]
    public async Task StageAsync_GenesisPlusGx_WritesCoreAndTemplate()
    {
        var title = await new EmbeddedRetroArchCores().StageAsync(GenesisPlusGx, _root);

        Assert.IsTrue(title.Exists, "code, content and meta");
        var rpx = Path.Combine(title.Code, "genesis_plus_gx_libretro.rpx");
        Assert.IsTrue(File.Exists(rpx));
        Assert.AreEqual(GenesisPlusGxLength, new FileInfo(rpx).Length);
        using (var stream = File.OpenRead(rpx))
        {
            var magic = new byte[4];
            Assert.AreEqual(4, stream.Read(magic, 0, 4));
            CollectionAssert.AreEqual(new byte[] { 0x7F, (byte)'E', (byte)'L', (byte)'F' }, magic);
        }
        Assert.IsTrue(File.Exists(title.AppXmlPath));
        Assert.IsTrue(File.Exists(Path.Combine(title.Code, CosXml.FileName)));
        Assert.IsTrue(File.Exists(title.MetaXmlPath));
        foreach (var file in RetroArchTemplate.MetaFiles)
            Assert.IsTrue(File.Exists(Path.Combine(title.Root, file.Replace('/', Path.DirectorySeparatorChar))), file);
        Assert.IsTrue(new BaseInspection(title).Layout().Passed);
        Assert.AreEqual(0, new RetroArchRomInjector(SourceConsole.Genesis).Inspect(title).Count);
    }

    [TestMethod]
    public async Task StageAsync_NullCore_ThrowsArgumentNullException()
    {
        var cores = new EmbeddedRetroArchCores();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(() => cores.StageAsync(null!, _root));
    }

    [TestMethod]
    public async Task StageAsync_PreCancelledToken_ThrowsOperationCanceledException()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cores = new EmbeddedRetroArchCores();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => cores.StageAsync(GenesisPlusGx, _root, cancellation.Token));
    }
}
