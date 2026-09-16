using System.IO.Compression;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Infrastructure.Tests;

[TestClass]
public class UwuvciKeyFileTests
{
    /// <summary>
    /// A List of TKeys as BinaryFormatter wrote it, built by hand with made-up keys: two good entries and one with a key that is not hex.
    /// </summary>
    internal const string SamplePayload =
        "AAEAAAD/////AQAAAAAAAAAMAgAAAEVVV1VWQ0kgQUlPIFdQRiwgVmVyc2lvbj0xLjAuMC4zLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPW51" +
        "bGwMAwAAAEtHYW1lQmFzZUNsYXNzTGlicmFyeSwgVmVyc2lvbj0xLjAuMC4wLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPW51bGwFAQAAAIgB" +
        "U3lzdGVtLkNvbGxlY3Rpb25zLkdlbmVyaWMuTGlzdGAxW1tVV1VWQ0lfQUlPX1dQRi5DbGFzc2VzLlRLZXlzLCBVV1VWQ0kgQUlPIFdQRiwgVmVyc2lvbj0x" +
        "LjAuMC4zLCBDdWx0dXJlPW5ldXRyYWwsIFB1YmxpY0tleVRva2VuPW51bGxdXQMAAAAGX2l0ZW1zBV9zaXplCF92ZXJzaW9uBAAAHlVXVVZDSV9BSU9fV1BG" +
        "LkNsYXNzZXMuVEtleXNbXQIAAAAICAIAAAAJBAAAAAMAAAADAAAAEAQAAAADAAAACQoAAAAJCwAAAAkMAAAABQoAAAAcVVdVVkNJX0FJT19XUEYuQ2xhc3Nl" +
        "cy5US2V5cwIAAAAVPEJhc2U+a19fQmFja2luZ0ZpZWxkFTxUa2V5PmtfX0JhY2tpbmdGaWVsZAQBHkdhbWVCYXNlQ2xhc3NMaWJyYXJ5LkdhbWVCYXNlcwMA" +
        "AAACAAAACQ0AAAAGDgAAACAwMDExMjIzMzQ0NTU2Njc3ODg5OWFhYmJjY2RkZWVmZgUNAAAAHkdhbWVCYXNlQ2xhc3NMaWJyYXJ5LkdhbWVCYXNlcwUAAAAV" +
        "PE5hbWU+a19fQmFja2luZ0ZpZWxkFzxSZWdpb24+a19fQmFja2luZ0ZpZWxkFTxQYXRoPmtfX0JhY2tpbmdGaWVsZBQ8VGlkPmtfX0JhY2tpbmdGaWVsZBg8" +
        "S2V5SGFzaD5rX19CYWNraW5nRmllbGQBBAEBARxHYW1lQmFzZUNsYXNzTGlicmFyeS5SZWdpb25zAwAAAAMAAAAGEAAAAAlEci4gTWFyaW8JDwAAAAYRAAAA" +
        "EkM6XGJhc2VzXERyLiBNYXJpbwYSAAAAEDAwMDUwMDAwMTAxNTMxMDAGEwAAAARoYXNoBQ8AAAAcR2FtZUJhc2VDbGFzc0xpYnJhcnkuUmVnaW9ucwEAAAAH" +
        "dmFsdWVfXwAIAwAAAAEAAAAFCwAAABxVV1VWQ0lfQUlPX1dQRi5DbGFzc2VzLlRLZXlzAgAAABU8QmFzZT5rX19CYWNraW5nRmllbGQVPFRrZXk+a19fQmFj" +
        "a2luZ0ZpZWxkBAEeR2FtZUJhc2VDbGFzc0xpYnJhcnkuR2FtZUJhc2VzAwAAAAIAAAAJFAAAAAYVAAAAIGZmZWVkZGNjYmJhYTk5ODg3NzY2NTU0NDMzMjIx" +
        "MTAwBRQAAAAeR2FtZUJhc2VDbGFzc0xpYnJhcnkuR2FtZUJhc2VzBQAAABU8TmFtZT5rX19CYWNraW5nRmllbGQXPFJlZ2lvbj5rX19CYWNraW5nRmllbGQV" +
        "PFBhdGg+a19fQmFja2luZ0ZpZWxkFDxUaWQ+a19fQmFja2luZ0ZpZWxkGDxLZXlIYXNoPmtfX0JhY2tpbmdGaWVsZAEEAQEBHEdhbWVCYXNlQ2xhc3NMaWJy" +
        "YXJ5LlJlZ2lvbnMDAAAAAwAAAAYXAAAACURyLiBNYXJpbwkWAAAABhgAAAASQzpcYmFzZXNcRHIuIE1hcmlvBhkAAAAQMDAwNTAwMDAxMDE1MzIwMAYaAAAA" +
        "BGhhc2gFFgAAABxHYW1lQmFzZUNsYXNzTGlicmFyeS5SZWdpb25zAQAAAAd2YWx1ZV9fAAgDAAAAAgAAAAUMAAAAHFVXVVZDSV9BSU9fV1BGLkNsYXNzZXMu" +
        "VEtleXMCAAAAFTxCYXNlPmtfX0JhY2tpbmdGaWVsZBU8VGtleT5rX19CYWNraW5nRmllbGQEAR5HYW1lQmFzZUNsYXNzTGlicmFyeS5HYW1lQmFzZXMDAAAA" +
        "AgAAAAkbAAAABhwAAAAJbm90LWEta2V5BRsAAAAeR2FtZUJhc2VDbGFzc0xpYnJhcnkuR2FtZUJhc2VzBQAAABU8TmFtZT5rX19CYWNraW5nRmllbGQXPFJl" +
        "Z2lvbj5rX19CYWNraW5nRmllbGQVPFBhdGg+a19fQmFja2luZ0ZpZWxkFDxUaWQ+a19fQmFja2luZ0ZpZWxkGDxLZXlIYXNoPmtfX0JhY2tpbmdGaWVsZAEE" +
        "AQEBHEdhbWVCYXNlQ2xhc3NMaWJyYXJ5LlJlZ2lvbnMDAAAAAwAAAAYeAAAABUtpcmJ5CR0AAAAGHwAAAA5DOlxiYXNlc1xLaXJieQYgAAAAEDAwMDUwMDAw" +
        "MTAxMDc3MDAGIQAAAARoYXNoBR0AAAAcR2FtZUJhc2VDbGFzc0xpYnJhcnkuUmVnaW9ucwEAAAAHdmFsdWVfXwAIAwAAAAAAAAAL";

    private string _root = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "UwuvciKeyFileTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void Parse_SerializedList_ReadsTitleIdsAndKeysAndSkipsBadOnes()
    {
        var keys = UwuvciKeyFile.Parse(Convert.FromBase64String(SamplePayload));

        Assert.AreEqual(2, keys.Count);
        Assert.AreEqual(TitleId.Parse("0005000010153100"), keys[0].TitleId);
        Assert.AreEqual("00112233445566778899aabbccddeeff", keys[0].Key.ToString());
        Assert.AreEqual(TitleId.Parse("0005000010153200"), keys[1].TitleId);
        Assert.AreEqual("ffeeddccbbaa99887766554433221100", keys[1].Key.ToString());
    }

    [TestMethod]
    public void Read_GzippedFile_Unpacks()
    {
        var path = Path.Combine(_root, "nes.vck");
        using (var file = File.Create(path))
        using (var gzip = new GZipStream(file, CompressionMode.Compress))
            gzip.Write(Convert.FromBase64String(SamplePayload), 0, Convert.FromBase64String(SamplePayload).Length);

        Assert.AreEqual(2, UwuvciKeyFile.Read(path).Count);
    }

    [TestMethod]
    public void Read_And_Parse_BadInput_Throw()
    {
        var plain = Path.Combine(_root, "plain.vck");
        File.WriteAllBytes(plain, new byte[] { 1, 2, 3, 4 });

        Assert.ThrowsExactly<InvalidDataException>(() => UwuvciKeyFile.Read(plain));
        Assert.ThrowsExactly<InvalidDataException>(() => UwuvciKeyFile.Parse(new byte[] { 0, 1, 2 }));
        Assert.ThrowsExactly<InvalidDataException>(() => UwuvciKeyFile.Parse(Array.Empty<byte>()));
        Assert.ThrowsExactly<ArgumentNullException>(() => UwuvciKeyFile.Parse(null!));
        Assert.ThrowsExactly<ArgumentException>(() => UwuvciKeyFile.Read(" "));
    }
}
