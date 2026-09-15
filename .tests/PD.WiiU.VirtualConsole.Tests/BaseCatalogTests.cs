using System.Text;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class BaseCatalogTests
{
    private const string Json = "[{\"titleId\":\"00050000101BAF00\",\"name\":\"Majora\",\"region\":\"UnitedStates\",\"console\":\"N64\"},{\"titleId\":\"000500001019D800\",\"name\":\"Xenoblade\",\"region\":\"UnitedStates\",\"console\":\"Wii\"}]";

    [TestMethod]
    public void Bundled_Always_ListsEveryConsoleWithUniqueIds()
    {
        var catalog = BaseCatalog.Bundled();

        Assert.IsTrue(catalog.Titles.Count >= 120, catalog.Titles.Count.ToString());
        Assert.AreEqual(catalog.Titles.Count, catalog.Titles.Select(t => t.TitleId).Distinct().Count());
        foreach (var console in new[] { SourceConsole.Nes, SourceConsole.Snes, SourceConsole.N64, SourceConsole.Gba, SourceConsole.Nds, SourceConsole.Tg16, SourceConsole.Msx, SourceConsole.Wii, SourceConsole.GameCube })
            Assert.IsTrue(catalog.For(console).Count > 0, console.ToString());
        Assert.IsTrue(catalog.Titles.All(t => t.TitleId.Type == TitleType.Game));
        Assert.IsTrue(catalog.Titles.All(t => t.Region is Region.Japan or Region.UnitedStates or Region.Europe));
        Assert.AreEqual("The Legend of Zelda: Majora's Mask", catalog.Find(TitleId.Parse("00050000101BAF00"))!.Name);
    }

    [TestMethod]
    public void For_GameCube_RetagsWiiBases()
    {
        var catalog = BaseCatalog.Parse(Stream(Json));

        var gameCube = catalog.For(SourceConsole.GameCube);

        Assert.AreEqual(1, gameCube.Count);
        Assert.AreEqual(SourceConsole.GameCube, gameCube[0].Console);
        Assert.AreEqual(TitleId.Parse("000500001019D800"), gameCube[0].TitleId);
        Assert.AreEqual(SourceConsole.Wii, catalog.For(SourceConsole.Wii).Single().Console);
        Assert.AreEqual(0, catalog.For(SourceConsole.Nes).Count);
    }

    [TestMethod]
    public void Find_KnownAndUnknownIds_ReturnsEntryOrNull()
    {
        var catalog = BaseCatalog.Parse(Stream(Json));

        Assert.AreEqual("Majora", catalog.Find(TitleId.Parse("00050000101BAF00"))!.Name);
        Assert.IsNull(catalog.Find(new TitleId(TitleType.Game, 1)));
    }

    [TestMethod]
    public void Load_File_ReadsIt()
    {
        var path = Path.Combine(TestTitle.TempRoot(), "bases.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, Json);
        try
        {
            Assert.AreEqual(2, BaseCatalog.Load(path).Titles.Count);
            Assert.ThrowsExactly<ArgumentException>(() => BaseCatalog.Load(" "));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [TestMethod]
    public void Parse_BadInput_ThrowsInvalidDataException()
    {
        Assert.ThrowsExactly<InvalidDataException>(() => BaseCatalog.Parse(Stream("[{\"titleId\":\"xyz\",\"name\":\"a\",\"region\":\"Japan\",\"console\":\"Nes\"}]")));
        Assert.ThrowsExactly<InvalidDataException>(() => BaseCatalog.Parse(Stream("[{\"titleId\":\"00050000101BAF00\",\"name\":\"\",\"region\":\"Japan\",\"console\":\"Nes\"}]")));
        Assert.ThrowsExactly<InvalidDataException>(() => BaseCatalog.Parse(Stream("[{\"titleId\":\"00050000101BAF00\",\"name\":\"a\",\"console\":\"Nes\"}]")));
        Assert.ThrowsExactly<InvalidDataException>(() => BaseCatalog.Parse(Stream("[{\"titleId\":\"00050000101BAF00\",\"name\":\"a\",\"region\":\"Mars\",\"console\":\"Nes\"}]")));
        Assert.ThrowsExactly<InvalidDataException>(() => BaseCatalog.Parse(Stream("not json")));
        Assert.ThrowsExactly<InvalidDataException>(() => BaseCatalog.Parse(Stream("null")));
        Assert.ThrowsExactly<ArgumentNullException>(() => BaseCatalog.Parse(null!));
    }

    [TestMethod]
    public void Constructor_DuplicateOrNullEntries_ThrowsArgumentException()
    {
        var title = TestTitle.Base();

        Assert.ThrowsExactly<ArgumentException>(() => new BaseCatalog(new[] { title, title }));
        Assert.ThrowsExactly<ArgumentException>(() => new BaseCatalog(new BaseTitle[] { null! }));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BaseCatalog(null!));
        Assert.AreEqual(0, new BaseCatalog(Array.Empty<BaseTitle>()).Titles.Count);
    }

    private static MemoryStream Stream(string json) => new(Encoding.UTF8.GetBytes(json));
}
