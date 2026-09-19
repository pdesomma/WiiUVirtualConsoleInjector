namespace PD.WiiU.VirtualConsole.RetroArch.Tests;

[TestClass]
public class EmbeddedRetroArchCoresTests
{
    private const int GenesisPlusGxLength = 6729787;

    private string _root = null!;

    private static RetroArchCore GenesisPlusGx => EmbeddedRetroArchCores.All.Single(core => core.Id == "genesis_plus_gx" && core.Console == SourceConsole.Genesis);

    [TestInitialize]
    public void Initialize() => _root = TestPaths.TempRoot();

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void All_Always_HasThreeGenesisCoresInDeclarationOrder()
    {
        var genesis = EmbeddedRetroArchCores.All.Where(core => core.Console == SourceConsole.Genesis).ToArray();

        Assert.AreEqual(3, genesis.Length);
        CollectionAssert.AreEqual(new[] { "genesis_plus_gx", "genesis_plus_gx_wide", "picodrive" }, genesis.Select(core => core.Id).ToArray());
    }

    [TestMethod]
    public void All_Always_RecommendsNothing()
    {
        Assert.IsFalse(EmbeddedRetroArchCores.All.Any(core => core.IsRecommended));
    }

    [TestMethod]
    public void All_EveryConsole_HasACoreAndASystem()
    {
        var cores = new EmbeddedRetroArchCores();
        foreach (var console in EmbeddedRetroArchCores.All.Select(core => core.Console).Distinct())
        {
            Assert.IsTrue(cores.Available(console).Count >= 1, console.ToString());
            Assert.IsNotNull(cores.System(console), console.ToString());
        }
        foreach (var system in EmbeddedRetroArchCores.Systems)
            Assert.IsTrue(cores.Available(system.Console).Count >= 1, system.Console.ToString());
    }

    [TestMethod]
    public void Available_Arcade_ListsTenCoresFbneoFirst()
    {
        var cores = new EmbeddedRetroArchCores();

        CollectionAssert.AreEqual(
            new[] { "fbneo", "mame2003_plus", "mame2010", "mame2000", "mame2003_midway", "fbalpha2012", "fbalpha2012_cps1", "fbalpha2012_cps2", "fbalpha2012_cps3", "fbalpha2012_neogeo" },
            cores.Available(SourceConsole.Arcade).Select(core => core.Id).ToArray());
        CollectionAssert.AreEqual(new[] { ".zip", ".7z" }, cores.System(SourceConsole.Arcade)!.Extensions.ToArray());
        Assert.AreEqual(0, cores.System(SourceConsole.Arcade)!.BiosFiles.Count);
    }

    [TestMethod]
    public void Available_NeoGeo_IsFbneoOnlyWithNoCardBios()
    {
        var cores = new EmbeddedRetroArchCores();

        var neoGeo = cores.Available(SourceConsole.NeoGeo);
        Assert.AreEqual(1, neoGeo.Count);
        Assert.AreEqual("fbneo", neoGeo[0].Id);
        Assert.AreEqual("fbneo_libretro.rpx", neoGeo[0].RpxFileName);
        StringAssert.Contains(neoGeo[0].Description, "neogeo.zip");
        CollectionAssert.AreEqual(new[] { ".zip", ".7z" }, cores.System(SourceConsole.NeoGeo)!.Extensions.ToArray());
        Assert.AreEqual(0, cores.System(SourceConsole.NeoGeo)!.BiosFiles.Count);
    }

    [TestMethod]
    public void Available_MasterSystemAndGameGear_ShareGenesisPlusGxWithGearsystem()
    {
        var cores = new EmbeddedRetroArchCores();
        foreach (var console in new[] { SourceConsole.MasterSystem, SourceConsole.GameGear })
            CollectionAssert.AreEqual(new[] { "genesis_plus_gx", "gearsystem" }, cores.Available(console).Select(core => core.Id).ToArray(), console.ToString());
        CollectionAssert.AreEqual(new[] { "picodrive" }, cores.Available(SourceConsole.Sega32X).Select(core => core.Id).ToArray());
    }

    [TestMethod]
    public void System_AtariLynx_NeedsTheBootRom()
    {
        var cores = new EmbeddedRetroArchCores();

        CollectionAssert.AreEqual(new[] { "lynxboot.img" }, cores.System(SourceConsole.AtariLynx)!.BiosFiles.ToArray());
        Assert.AreEqual(0, cores.System(SourceConsole.Atari2600)!.BiosFiles.Count);
        Assert.AreEqual(0, cores.System(SourceConsole.Atari7800)!.BiosFiles.Count);
        CollectionAssert.AreEqual(new[] { "stella2023" }, cores.Available(SourceConsole.Atari2600).Select(c => c.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "prosystem" }, cores.Available(SourceConsole.Atari7800).Select(c => c.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "handy" }, cores.Available(SourceConsole.AtariLynx).Select(c => c.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "mednafen_vb" }, cores.Available(SourceConsole.VirtualBoy).Select(c => c.Id).ToArray());
        CollectionAssert.AreEqual(new[] { ".vb", ".vboy" }, cores.System(SourceConsole.VirtualBoy)!.Extensions.ToArray());
    }

    [TestMethod]
    public void System_KnownAndUnknown()
    {
        var cores = new EmbeddedRetroArchCores();
        CollectionAssert.AreEqual(new[] { ".sms" }, cores.System(SourceConsole.MasterSystem)!.Extensions.ToArray());
        CollectionAssert.AreEqual(new[] { ".gg" }, cores.System(SourceConsole.GameGear)!.Extensions.ToArray());
        CollectionAssert.AreEqual(new[] { ".32x", ".bin" }, cores.System(SourceConsole.Sega32X)!.Extensions.ToArray());
        Assert.IsTrue(cores.System(SourceConsole.Genesis)!.Accepts("game.MD"));
        Assert.IsNull(cores.System(SourceConsole.Nes));
    }

    [TestMethod]
    public void All_ArcadeCores_AreEmbeddedAtTheCatalogedSizes()
    {
        var sizes = new Dictionary<string, long>
        {
            ["fbneo"] = 29751521,
            ["mame2003_plus"] = 18012178,
            ["mame2010"] = 24833358,
            ["mame2000"] = 10261117,
            ["mame2003_midway"] = 6345351,
            ["fbalpha2012"] = 12335794,
            ["fbalpha2012_cps1"] = 6054951,
            ["fbalpha2012_cps2"] = 5944447,
            ["fbalpha2012_cps3"] = 5506159,
            ["fbalpha2012_neogeo"] = 6099616,
        };

        foreach (var core in EmbeddedRetroArchCores.All.Where(core => core.Console == SourceConsole.Arcade))
        {
            using var stream = typeof(EmbeddedRetroArchCores).Assembly.GetManifestResourceStream(EmbeddedRetroArchCores.ResourceName(core));
            Assert.IsNotNull(stream, core.Id);
            Assert.AreEqual(sizes[core.Id], stream!.Length, core.Id);
        }
    }

    [TestMethod]
    public void All_EveryCore_HasItsResourceCompiledIn()
    {
        var names = typeof(EmbeddedRetroArchCores).Assembly.GetManifestResourceNames();

        foreach (var core in EmbeddedRetroArchCores.All)
            CollectionAssert.Contains(names, EmbeddedRetroArchCores.ResourceName(core), core.Id);
    }

    [TestMethod]
    public void Available_Genesis_KeepsDeclarationOrder()
    {
        var cores = new EmbeddedRetroArchCores().Available(SourceConsole.Genesis);

        Assert.AreEqual(3, cores.Count);
        Assert.AreEqual("genesis_plus_gx", cores[0].Id);
        Assert.AreEqual("picodrive", cores[2].Id);
        Assert.IsFalse(cores.Any(core => core.IsRecommended));
    }

    [TestMethod]
    public void Available_Nes_IsEmpty()
    {
        Assert.AreEqual(0, new EmbeddedRetroArchCores().Available(SourceConsole.Nes).Count);
    }

    [TestMethod]
    public void ResourceName_GenesisPlusGx_IsCoresPath()
    {
        Assert.AreEqual("cores/genesis_plus_gx_libretro.rpx", EmbeddedRetroArchCores.ResourceName(GenesisPlusGx));
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
