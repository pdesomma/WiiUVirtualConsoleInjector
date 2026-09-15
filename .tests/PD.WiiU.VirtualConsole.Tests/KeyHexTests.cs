namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class KeyHexTests
{
    [TestMethod]
    public void Parse_And_Format_RoundTrip()
    {
        var bytes = KeyHex.Parse(" 00FFa5 ", 3);

        CollectionAssert.AreEqual(new byte[] { 0x00, 0xFF, 0xA5 }, bytes);
        Assert.AreEqual("00ffa5", KeyHex.Format(bytes));
    }

    [TestMethod]
    public void Parse_BadInput_Throws()
    {
        Assert.ThrowsExactly<FormatException>(() => KeyHex.Parse("00ff", 3));
        Assert.ThrowsExactly<FormatException>(() => KeyHex.Parse("zz", 1));
        Assert.ThrowsExactly<ArgumentNullException>(() => KeyHex.Parse(null!, 1));
        Assert.ThrowsExactly<ArgumentNullException>(() => KeyHex.Format(null!));
    }
}
