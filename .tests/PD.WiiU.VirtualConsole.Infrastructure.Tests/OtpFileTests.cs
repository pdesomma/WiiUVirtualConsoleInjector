using PD.WiiU.VirtualConsole.Infrastructure;
using WiiUSharp.Nus;

namespace PD.WiiU.VirtualConsole.Infrastructure.Tests;

[TestClass]
public class OtpFileTests
{
    private const int OtpSize = 0x400;

    private string _root = null!;
    private string _path = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Infrastructure.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _path = Path.Combine(_root, "otp.bin");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void CommonKeyOffset_IsE0()
    {
        Assert.AreEqual(0xE0, OtpFile.CommonKeyOffset);
    }

    [TestMethod]
    public void ReadCommonKey_BlankPath_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => OtpFile.ReadCommonKey(""));
    }

    [TestMethod]
    public void ReadCommonKey_ExactlyLongEnough_ReadsKey()
    {
        var otp = new byte[OtpFile.CommonKeyOffset + 16];
        var key = Enumerable.Range(1, 16).Select(i => (byte)(i * 5)).ToArray();
        key.CopyTo(otp, OtpFile.CommonKeyOffset);
        File.WriteAllBytes(_path, otp);

        Assert.AreEqual(new CommonKey(key), OtpFile.ReadCommonKey(_path));
    }

    [TestMethod]
    public void ReadCommonKey_FullDump_ReadsBytesAtOffset()
    {
        var otp = new byte[OtpSize];
        for (var i = 0; i < otp.Length; i++)
            otp[i] = (byte)i;
        File.WriteAllBytes(_path, otp);

        var key = OtpFile.ReadCommonKey(_path);

        CollectionAssert.AreEqual(Enumerable.Range(OtpFile.CommonKeyOffset, 16).Select(i => (byte)i).ToArray(), key.ToArray());
    }

    [TestMethod]
    public void ReadCommonKey_MissingFile_ThrowsFileNotFoundException()
    {
        Assert.ThrowsExactly<FileNotFoundException>(() => OtpFile.ReadCommonKey(_path));
    }

    [TestMethod]
    public void ReadCommonKey_TooShort_ThrowsInvalidDataException()
    {
        File.WriteAllBytes(_path, new byte[OtpFile.CommonKeyOffset + 15]);

        Assert.ThrowsExactly<InvalidDataException>(() => OtpFile.ReadCommonKey(_path));
    }
}
