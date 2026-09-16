namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class ByteSizeTests
{
    [TestMethod]
    public void Text_EachUnit_MatchesTheConsole()
    {
        Assert.AreEqual("0 KB", new ByteSize(0).Text);
        Assert.AreEqual("64 KB", new ByteSize(0x10010).Text);
        Assert.AreEqual("384 KB", new ByteSize(393232).Text);
        Assert.AreEqual("1 MB", new ByteSize(0x100010).Text);
        Assert.AreEqual("1.2 MB", new ByteSize(1258291).Text);
        Assert.AreEqual("4 MB", new ByteSize(0x400000).Text);
        Assert.AreEqual("1.23 GB", new ByteSize(1325989888).Text);
        Assert.AreEqual("4.41 GB", new ByteSize(4738154496).Text);
        Assert.AreEqual(ByteSizeUnit.Kilobytes, new ByteSize(1048575).Unit);
        Assert.AreEqual(ByteSizeUnit.Megabytes, new ByteSize(1048576).Unit);
        Assert.AreEqual(ByteSizeUnit.Gigabytes, new ByteSize(1073741824).Unit);
        Assert.AreEqual("4 MB", new ByteSize(0x400000).ToString());
        Assert.IsTrue(new ByteSize(1).CompareTo(new ByteSize(2)) < 0);
    }

    [TestMethod]
    public void OfDirectory_And_OfFile_SumWhatIsThere()
    {
        var root = Path.Combine(Path.GetTempPath(), "ByteSizeTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "sub"));
        File.WriteAllBytes(Path.Combine(root, "a.bin"), new byte[1000]);
        File.WriteAllBytes(Path.Combine(root, "sub", "b.bin"), new byte[24]);
        try
        {
            Assert.AreEqual(1024, ByteSize.OfDirectory(root).Bytes);
            Assert.AreEqual(1000, ByteSize.OfFile(Path.Combine(root, "a.bin")).Bytes);
            Assert.AreEqual(0, ByteSize.OfDirectory(Path.Combine(root, "nope")).Bytes);
            Assert.AreEqual(0, ByteSize.OfFile(Path.Combine(root, "nope.bin")).Bytes);
            Assert.AreEqual(0, ByteSize.OfDirectory("").Bytes);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
