using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class GameFactoryTests
{
    [TestMethod]
    public void Create_Identity_KeepsItsIdsAndProductCode()
    {
        var identity = new TitleIdentity(new TitleId(TitleType.Demo, 0x31323334), new GroupId(0x3456), new ProductCode(ProductCode.EShop, "WXYZ"));

        var game = GameFactory.Create("Again", identity: identity, random: new Random(7));

        Assert.AreEqual(identity.TitleId, game.TitleId);
        Assert.AreEqual(identity.GroupId, game.GroupId);
        Assert.AreEqual(identity.ProductCode, game.ProductCode);
        Assert.AreEqual(identity, TitleIdentity.Of(game));
    }

    [TestMethod]
    public void Create_IdentityAndProductId_TakesTheTypedProductIdOverTheIdentitys()
    {
        var identity = new TitleIdentity(new TitleId(TitleType.Demo, 0x31323334), new GroupId(0x3456), new ProductCode(ProductCode.EShop, "WXYZ"));

        var game = GameFactory.Create("Again", productId: "QRST", identity: identity);

        Assert.AreEqual(identity.TitleId, game.TitleId);
        Assert.AreEqual("QRST", game.ProductCode.Id);
    }

    [TestMethod]
    public void TitleIdentity_Of_Null_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => TitleIdentity.Of(null!));
    }

    [TestMethod]
    public void Create_Name_MakesDemoTitleWithRandomIdsAboveTheFloor()
    {
        var game = GameFactory.Create("Super Metroid", random: new Random(7));

        Assert.AreEqual(TitleType.Demo, game.TitleId.Type);
        Assert.IsTrue((game.TitleId.UniqueId >> 16) >= GameFactory.MinimumIdHalf);
        Assert.IsTrue((game.TitleId.UniqueId & 0xFFFF) >= GameFactory.MinimumIdHalf);
        Assert.IsTrue(game.GroupId.Value >= GameFactory.MinimumIdHalf && game.GroupId.Value <= 0xFFFF);
        Assert.AreEqual(ProductCode.EShop, game.ProductCode.Category);
        Assert.AreEqual(game.GroupId.Value.ToString("X4"), game.ProductCode.Id);
        Assert.IsNull(game.GamePadUse, "the base's drc_use is kept");
        Assert.AreEqual("Super Metroid", game.NameIn(Language.English)!.ShortName);
        Assert.AreEqual("Super Metroid", game.NameIn(Language.Japanese)!.LongName);
    }

    [TestMethod]
    public void Create_CommaInName_SplitsLongAndShortNames()
    {
        var game = GameFactory.Create(" The Legend of Zelda, Majora's Mask ", productId: "ZELD", gamePad: true);

        Assert.AreEqual("The Legend of Zelda", game.NameIn(Language.English)!.ShortName);
        Assert.AreEqual("The Legend of Zelda\nMajora's Mask", game.NameIn(Language.English)!.LongName);
        Assert.AreEqual("ZELD", game.ProductCode.Id);
        Assert.AreEqual(65537u, game.GamePadUse);
    }

    [TestMethod]
    public void Create_SameSeed_IsDeterministicAndDifferentSeedsDiffer()
    {
        var a = GameFactory.Create("x", random: new Random(1));
        var b = GameFactory.Create("x", random: new Random(1));
        var c = GameFactory.Create("x", random: new Random(2));

        Assert.AreEqual(a.TitleId, b.TitleId);
        Assert.AreNotEqual(a.TitleId, c.TitleId);
    }

    [TestMethod]
    public void Create_BadArguments_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => GameFactory.Create(" "));
        Assert.ThrowsExactly<ArgumentException>(() => GameFactory.Create("x", productId: "TOOLONG"));
    }
    [TestMethod]
    public void Create_ShortNameGiven_UsesItInsteadOfTheFirstSegment()
    {
        var game = GameFactory.Create("The Legend of Zelda, Majora's Mask", "  Majora's Mask  ");

        var name = game.NameIn(Language.English)!;
        Assert.AreEqual("Majora's Mask", name.ShortName);
        Assert.AreEqual("The Legend of Zelda\nMajora's Mask", name.LongName);
    }

    [TestMethod]
    public void Create_BlankShortName_FallsBackToTheFirstSegment()
    {
        foreach (var shortName in new[] { null, "", "   " })
        {
            var name = GameFactory.Create("The Legend of Zelda, Majora's Mask", shortName).NameIn(Language.English)!;

            Assert.AreEqual("The Legend of Zelda", name.ShortName);
        }
    }

}
