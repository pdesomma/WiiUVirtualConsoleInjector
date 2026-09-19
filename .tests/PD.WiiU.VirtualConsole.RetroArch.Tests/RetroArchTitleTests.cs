using WiiUSharp;

namespace PD.WiiU.VirtualConsole.RetroArch.Tests;

[TestClass]
public class RetroArchTitleTests
{
    private string _root = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = TestPaths.TempRoot();
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public async Task StageInjectApply_RealCore_ProducesParseableTitleWithGameIdentity()
    {
        var core = EmbeddedRetroArchCores.All.Single(c => c.Id == "genesis_plus_gx" && c.Console == SourceConsole.Genesis);
        var romPath = Path.Combine(_root, "Sonic (U).md");
        var bytes = Enumerable.Range(0, 512).Select(i => (byte)i).ToArray();
        File.WriteAllBytes(romPath, bytes);
        var game = GameFactory.Create("Sonic, The Hedgehog", null, null, false, new Random(7));
        var injection = new Injection(core, new Rom(romPath, SourceConsole.Genesis), game);

        var title = await new EmbeddedRetroArchCores().StageAsync(core, Path.Combine(_root, "title"));
        await new RetroArchRomInjector(SourceConsole.Genesis).InjectAsync(injection, title);
        var meta = MetaXml.Load(title.MetaXmlPath);
        meta.Apply(game);
        meta.Save(title.MetaXmlPath);
        var app = AppXml.Load(title.AppXmlPath);
        app.Apply(game);
        app.Save(title.AppXmlPath);

        Assert.IsTrue(new BaseInspection(title).Layout().Passed);
        Assert.AreEqual(0, new RetroArchRomInjector(SourceConsole.Genesis).Inspect(title).Count);
        Assert.AreEqual(game.TitleId.ToString(), AppXml.Load(title.AppXmlPath).ReadTitleId().ToString());
        Assert.AreEqual(game.TitleId, MetaXml.Load(title.MetaXmlPath).Read().TitleId);
        CollectionAssert.AreEqual(bytes, File.ReadAllBytes(Path.Combine(title.Content, "Sonic_U.md")));
        Assert.AreEqual(core.RpxFileName + " --appendconfig " + RetroArchTemplate.ContentMount + "retroarch.cfg " + RetroArchTemplate.ContentMount + "Sonic_U.md", CosXml.Load(Path.Combine(title.Code, CosXml.FileName)).Arguments);
    }
}
