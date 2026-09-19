using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class InjectionRecordTests
{
    private static readonly DateTimeOffset When = new(2026, 9, 18, 9, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Constructor_BaseTemplate_RoundTripsTheKey()
    {
        var key = TemplateKey.Base(new TitleId(TitleType.Game, 0x101C9300));

        var record = new InjectionRecord("one", When, SourceConsole.N64, key, @"C:\roms\game.z64", "Game", Identity());

        Assert.AreEqual(key, record.Template);
        Assert.IsFalse(record.Template.IsCore);
        Assert.AreEqual(new TitleId(TitleType.Game, 0x101C9300), record.Template.BaseTitleId);
    }

    [TestMethod]
    public void Constructor_BlankRequiredText_ThrowsArgumentException()
    {
        var key = TemplateKey.Core("genesis_plus_gx");

        Assert.ThrowsExactly<ArgumentException>(() => new InjectionRecord(" ", When, SourceConsole.Genesis, key, @"C:\roms\game.md", "Game", Identity()));
        Assert.ThrowsExactly<ArgumentException>(() => new InjectionRecord("one", When, SourceConsole.Genesis, key, "", "Game", Identity()));
        Assert.ThrowsExactly<ArgumentException>(() => new InjectionRecord("one", When, SourceConsole.Genesis, key, @"C:\roms\game.md", null!, Identity()));
    }

    [TestMethod]
    public void Constructor_CoreTemplate_RoundTripsTheKey()
    {
        var key = TemplateKey.Core("genesis_plus_gx");

        var record = new InjectionRecord("one", When, SourceConsole.Genesis, key, @"C:\roms\game.md", "Game", Identity());

        Assert.AreEqual(key, record.Template);
        Assert.IsTrue(record.Template.IsCore);
        Assert.AreEqual("genesis_plus_gx", record.Template.CoreId);
        Assert.IsNull(record.Template.BaseTitleId);
    }

    [TestMethod]
    public void Constructor_NullIdentity_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new InjectionRecord("one", When, SourceConsole.Genesis, TemplateKey.Core("genesis_plus_gx"), @"C:\roms\game.md", "Game", null!));
    }

    [TestMethod]
    public void Constructor_NullTemplate_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new InjectionRecord("one", When, SourceConsole.Genesis, null!, @"C:\roms\game.md", "Game", Identity()));
    }

    [TestMethod]
    public void DisplayName_CommaInName_IsTheFirstPart()
    {
        var record = new InjectionRecord("one", When, SourceConsole.Genesis, TemplateKey.Core("genesis_plus_gx"), @"C:\roms\game.md", "Sonic, The Hedgehog", Identity());

        Assert.AreEqual("Sonic", record.DisplayName);
        Assert.AreEqual("Sonic (Genesis)", record.ToString());
    }

    private static TitleIdentity Identity() =>
        new(new TitleId(TitleType.Demo, 0x31323334), new GroupId(0x3456), new ProductCode(ProductCode.EShop, "WXYZ"));
}
