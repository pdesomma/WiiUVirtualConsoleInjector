namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public sealed class CardFileTests
{
    [TestMethod]
    public void Constructor_BackslashesAndSurroundingSlashes_Normalised()
    {
        var file = new CardFile(@"C:\dumps\lynxboot.img", @"\retroarch\system\lynxboot.img\");

        Assert.AreEqual("retroarch/system/lynxboot.img", file.CardPath);
        Assert.AreEqual("lynxboot.img", file.FileName);
        Assert.AreEqual(@"C:\dumps\lynxboot.img", file.SourcePath);
    }

    [TestMethod]
    public void Constructor_InvalidArguments_Throw()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new CardFile(" ", "retroarch/system/x"));
        Assert.ThrowsExactly<ArgumentException>(() => new CardFile(@"C:\x", " "));
        Assert.ThrowsExactly<ArgumentException>(() => new CardFile(@"C:\x", "/"));
        Assert.ThrowsExactly<ArgumentException>(() => new CardFile(@"C:\x", "retroarch/../x"));
        Assert.ThrowsExactly<ArgumentException>(() => new CardFile(@"C:\x", "retroarch//x"));
    }

    [TestMethod]
    public void On_Root_JoinsWithTheCardPath()
    {
        var file = new CardFile(@"C:\x", "retroarch/system/lynxboot.img");

        Assert.AreEqual(Path.Combine(@"E:\", "retroarch", "system", "lynxboot.img"), file.On(@"E:\"));
        Assert.ThrowsExactly<ArgumentException>(() => file.On(" "));
    }

    [TestMethod]
    public void Size_MissingSource_Zero()
    {
        Assert.AreEqual(0, new CardFile(@"C:\nope\missing.img", "retroarch/system/missing.img").Size.Bytes);
    }
}
