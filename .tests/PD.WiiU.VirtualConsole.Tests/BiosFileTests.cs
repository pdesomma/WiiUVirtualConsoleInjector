namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public sealed class BiosFileTests
{
    private string _folder = null!;

    [TestInitialize]
    public void Initialize()
    {
        _folder = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    [TestMethod]
    public void Constructor_BlankLabel_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new BiosFile(" ", "a.bin"));
        Assert.ThrowsExactly<ArgumentException>(() => new BiosFile(null!, "a.bin"));
    }

    [TestMethod]
    public void Constructor_BlankName_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new BiosFile("BIOS", "a.bin", " "));
        Assert.ThrowsExactly<ArgumentException>(() => new BiosFile("BIOS", "a.bin", null!));
    }

    [TestMethod]
    public void Constructor_LabelOnly_LabelDoublesAsTheName()
    {
        var bios = new BiosFile("lynxboot.img");

        Assert.AreEqual("lynxboot.img", bios.Label);
        CollectionAssert.AreEqual(new[] { "lynxboot.img" }, bios.Names.ToArray());
    }

    [TestMethod]
    public void Constructor_LabelAndNames_KeepsNamesInOrder()
    {
        var bios = new BiosFile("PlayStation BIOS", "scph5501.bin", "scph1001.bin");

        Assert.AreEqual("PlayStation BIOS", bios.Label);
        CollectionAssert.AreEqual(new[] { "scph5501.bin", "scph1001.bin" }, bios.Names.ToArray());
    }

    [TestMethod]
    public void Constructor_NullNames_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new BiosFile("BIOS", null!));
    }

    [TestMethod]
    public void NameFor_BlankPath_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new BiosFile("BIOS", "a.bin").NameFor(" "));
    }

    [TestMethod]
    public void NameFor_MatchingNameInAnyCase_KeepsIt()
    {
        var bios = new BiosFile("PlayStation BIOS", "scph5501.bin", "scph1001.bin");

        Assert.AreEqual("scph1001.bin", bios.NameFor(@"C:\dumps\SCPH1001.BIN"));
    }

    [TestMethod]
    public void NameFor_UnknownName_IsThePreferredOne()
    {
        var bios = new BiosFile("PlayStation BIOS", "scph5501.bin", "scph1001.bin");

        Assert.AreEqual("scph5501.bin", bios.NameFor(@"C:\dumps\bios.bin"));
    }

    [TestMethod]
    public void Present_BlankFolder_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new BiosFile("BIOS", "a.bin").Present(" "));
    }

    [TestMethod]
    public void Present_FirstAcceptedNameOnDisk_IsReturned()
    {
        var bios = new BiosFile("PlayStation BIOS", "scph5501.bin", "scph5500.bin", "scph1001.bin");
        File.WriteAllBytes(Path.Combine(_folder, "scph5500.bin"), new byte[] { 1 });
        File.WriteAllBytes(Path.Combine(_folder, "scph1001.bin"), new byte[] { 2 });

        Assert.AreEqual("scph5500.bin", bios.Present(_folder));
    }

    [TestMethod]
    public void Present_NoneOnDisk_IsNull()
    {
        Assert.IsNull(new BiosFile("PlayStation BIOS", "scph5501.bin", "scph1001.bin").Present(_folder));
        Assert.IsNull(new BiosFile("lynxboot.img").Present(Path.Combine(_folder, "missing")));
    }

    [TestMethod]
    public void ToString_Always_IsTheLabel()
    {
        Assert.AreEqual("PlayStation BIOS", new BiosFile("PlayStation BIOS", "scph5501.bin").ToString());
    }
}
