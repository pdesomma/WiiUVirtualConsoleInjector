using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class HexKeyTextTests
{
    [TestMethod]
    public void IsValid_ExactHex_ReturnsTrue()
    {
        Assert.IsTrue(HexKeyText.IsValid("0f1e2d3c4b5a69788796a5b4c3d2e1f0", 16));
        Assert.IsTrue(HexKeyText.IsValid("0F1E2D3C4B5A69788796A5B4C3D2E1F0", 16));
        Assert.IsTrue(HexKeyText.IsValid("  0f1e2d3c4b5a69788796a5b4c3d2e1f0\n", 16));
        Assert.IsTrue(HexKeyText.IsValid("abcd", 2));
    }

    [TestMethod]
    public void IsValid_WrongLengthOrNotHex_ReturnsFalse()
    {
        Assert.IsFalse(HexKeyText.IsValid("0f1e2d3c4b5a69788796a5b4c3d2e1f", 16));
        Assert.IsFalse(HexKeyText.IsValid("0f1e2d3c4b5a69788796a5b4c3d2e1f00", 16));
        Assert.IsFalse(HexKeyText.IsValid("0g1e2d3c4b5a69788796a5b4c3d2e1f0", 16));
        Assert.IsFalse(HexKeyText.IsValid("0f1e 2d3c4b5a69788796a5b4c3d2e1f0", 16));
    }

    [TestMethod]
    public void IsValid_NullOrEmpty_ReturnsFalse()
    {
        Assert.IsFalse(HexKeyText.IsValid(null, 16));
        Assert.IsFalse(HexKeyText.IsValid(string.Empty, 16));
        Assert.IsFalse(HexKeyText.IsValid("   ", 16));
    }
}
