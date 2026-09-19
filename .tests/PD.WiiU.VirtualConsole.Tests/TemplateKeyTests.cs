using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class TemplateKeyTests
{
    private static readonly TitleId Id = new(TitleType.Game, 0x101C9300);

    [TestMethod]
    public void Base_TitleId_IsNotACore()
    {
        var key = TemplateKey.Base(Id);

        Assert.AreEqual(Id, key.BaseTitleId);
        Assert.IsNull(key.CoreId);
        Assert.IsFalse(key.IsCore);
    }

    [TestMethod]
    public void Core_BlankId_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => TemplateKey.Core(""));
        Assert.ThrowsExactly<ArgumentException>(() => TemplateKey.Core("  "));
        Assert.ThrowsExactly<ArgumentException>(() => TemplateKey.Core(null!));
    }

    [TestMethod]
    public void Core_Id_IsACore()
    {
        var key = TemplateKey.Core("genesis_plus_gx");

        Assert.AreEqual("genesis_plus_gx", key.CoreId);
        Assert.IsNull(key.BaseTitleId);
        Assert.IsTrue(key.IsCore);
    }

    [TestMethod]
    public void Equals_DifferentKeys_AreNotEqual()
    {
        Assert.AreNotEqual(TemplateKey.Base(Id), TemplateKey.Base(new TitleId(TitleType.Game, 0x10101D00)));
        Assert.AreNotEqual(TemplateKey.Core("genesis_plus_gx"), TemplateKey.Core("picodrive"));
        Assert.AreNotEqual(TemplateKey.Core("genesis_plus_gx"), TemplateKey.Base(Id));
        Assert.IsFalse(TemplateKey.Base(Id).Equals(null));
        Assert.IsFalse(TemplateKey.Base(Id).Equals("not a key"));
    }

    [TestMethod]
    public void Equals_SameKeys_AreEqualWithTheSameHash()
    {
        Assert.AreEqual(TemplateKey.Base(Id), TemplateKey.Base(Id));
        Assert.AreEqual(TemplateKey.Base(Id).GetHashCode(), TemplateKey.Base(Id).GetHashCode());
        Assert.AreEqual(TemplateKey.Core("genesis_plus_gx"), TemplateKey.Core("genesis_plus_gx"));
        Assert.AreEqual(TemplateKey.Core("genesis_plus_gx").GetHashCode(), TemplateKey.Core("genesis_plus_gx").GetHashCode());
        Assert.IsTrue(TemplateKey.Core("genesis_plus_gx").Equals((object)TemplateKey.Core("genesis_plus_gx")));
    }

    [TestMethod]
    public void Of_BaseTitle_IsItsTitleId()
    {
        var key = TemplateKey.Of(TestTitle.Base());

        Assert.AreEqual(TemplateKey.Base(TestTitle.Base().TitleId), key);
    }

    [TestMethod]
    public void Of_Null_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => TemplateKey.Of(null!));
    }

    [TestMethod]
    public void Of_RetroArchCore_IsItsId()
    {
        var key = TemplateKey.Of(new RetroArchCore("picodrive", "PicoDrive", SourceConsole.Genesis, "Fast."));

        Assert.AreEqual(TemplateKey.Core("picodrive"), key);
    }

    [TestMethod]
    public void Of_UnknownTemplate_ThrowsNotSupportedException()
    {
        Assert.ThrowsExactly<NotSupportedException>(() => TemplateKey.Of(new OtherTemplate()));
    }

    [TestMethod]
    public void ToString_NamesTheIdOfEitherKind()
    {
        Assert.AreEqual(Id.ToString(), TemplateKey.Base(Id).ToString());
        Assert.AreEqual("genesis_plus_gx", TemplateKey.Core("genesis_plus_gx").ToString());
    }

    private sealed class OtherTemplate : ITitleTemplate
    {
        public SourceConsole Console => SourceConsole.Genesis;
        public string Name => "Other";
    }
}
