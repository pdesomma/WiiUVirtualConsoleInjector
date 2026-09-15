namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class AncastKeyTests
{
    [TestMethod]
    public void Parse_Hex_RoundTripsThroughToString()
    {
        var key = AncastKey.Parse("00112233445566778899AABBCCDDEEFF");

        Assert.AreEqual("00112233445566778899aabbccddeeff", key.ToString());
        Assert.AreEqual(key, new AncastKey(key.ToArray()));
        Assert.IsTrue(key != default);
        Assert.AreEqual(new AncastKey(new byte[16]), default);
        Assert.AreEqual("00000000000000000000000000000000", default(AncastKey).ToString());
    }

    [TestMethod]
    public void Parse_BadInput_Throws()
    {
        Assert.ThrowsExactly<FormatException>(() => AncastKey.Parse("abc"));
        Assert.ThrowsExactly<FormatException>(() => AncastKey.Parse("zz112233445566778899AABBCCDDEEFF"));
        Assert.ThrowsExactly<ArgumentNullException>(() => AncastKey.Parse(null!));
        Assert.ThrowsExactly<ArgumentException>(() => new AncastKey(new byte[3]));
        Assert.ThrowsExactly<ArgumentNullException>(() => new AncastKey(null!));
    }

    [TestMethod]
    public void ToArray_Always_ReturnsACopy()
    {
        var bytes = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();
        var key = new AncastKey(bytes);

        bytes[0] = 0xFF;
        key.ToArray()[1] = 0xFF;

        Assert.AreEqual(0, key.ToArray()[0]);
        Assert.AreEqual(1, key.ToArray()[1]);
    }
}
