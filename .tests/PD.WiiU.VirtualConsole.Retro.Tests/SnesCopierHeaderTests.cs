namespace PD.WiiU.VirtualConsole.Retro.Tests;

[TestClass]
public class SnesCopierHeaderTests
{
    [TestMethod]
    public void IsPresent_SizeIs512Over_True()
    {
        Assert.IsTrue(SnesCopierHeader.IsPresent(new byte[1024 * 1024 + 512]));
        Assert.IsTrue(SnesCopierHeader.IsPresent(new byte[1024 + 512]));
    }

    [TestMethod]
    public void IsPresent_WholeKilobytesOrTiny_False()
    {
        Assert.IsFalse(SnesCopierHeader.IsPresent(new byte[1024 * 1024]));
        Assert.IsFalse(SnesCopierHeader.IsPresent(new byte[512]));
        Assert.IsFalse(SnesCopierHeader.IsPresent(new byte[1000]));
        Assert.IsFalse(SnesCopierHeader.IsPresent(Array.Empty<byte>()));
    }

    [TestMethod]
    public void Strip_Headered_DropsTheFirst512Bytes()
    {
        var rom = Enumerable.Range(0, 2048 + 512).Select(i => (byte)i).ToArray();

        var bare = SnesCopierHeader.Strip(rom);

        Assert.AreEqual(2048, bare.Length);
        CollectionAssert.AreEqual(rom.Skip(512).ToArray(), bare);
    }

    [TestMethod]
    public void Strip_Headerless_ReturnsTheSameArray()
    {
        var rom = new byte[2048];

        Assert.AreSame(rom, SnesCopierHeader.Strip(rom));
    }

    [TestMethod]
    public void Null_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => SnesCopierHeader.IsPresent(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => SnesCopierHeader.Strip(null!));
    }
}
