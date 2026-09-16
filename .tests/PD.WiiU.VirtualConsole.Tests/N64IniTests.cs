namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class N64IniTests
{
    private const string Retail = ";Ocarina US\r\n[Cheat]\r\n;pause menu\r\nCheat0 = 2\r\nCheat0_Addr = 0x801DAE8B\r\n\r\n[RomOption]\r\nRetraceByVsync = 0\r\nRumble = 1\r\nPDFURL = \"http://m1.nintendo.net/docvc/NUS/USA/CZLE/CZLE_E.pdf\"\r\nUseTimer = 1\r\nRSPMultiCore = 0\r\n\r\n[Render]\r\nCopyDepthBuffer = 1\r\n";

    [TestMethod]
    public void Parse_RetailFile_SplitsKnownSettingsFromTheRest()
    {
        var ini = N64Ini.Parse(Retail);

        Assert.AreEqual("Ocarina US", ini.Comment);
        Assert.AreEqual(false, ini.RetraceByVsync);
        Assert.AreEqual(true, ini.Rumble);
        Assert.AreEqual(true, ini.UseTimer);
        Assert.AreEqual(false, ini.RspMultiCore);
        Assert.IsNull(ini.BackupType);
        Assert.IsNull(ini.BackupSize);
        Assert.IsNull(ini.ExpansionPak);
        StringAssert.StartsWith(ini.Extra, "[RomOption]", "an unknown RomOption key is kept");
        StringAssert.Contains(ini.Extra, "PDFURL = \"http://m1.nintendo.net/docvc/NUS/USA/CZLE/CZLE_E.pdf\"");
        StringAssert.Contains(ini.Extra, "[Cheat]");
        StringAssert.Contains(ini.Extra, "Cheat0_Addr = 0x801DAE8B");
        StringAssert.Contains(ini.Extra, "[Render]");
        StringAssert.Contains(ini.Extra, "CopyDepthBuffer = 1");
    }

    [TestMethod]
    public void ToText_ThenParse_RoundTrips()
    {
        var ini = new N64Ini
        {
            Comment = "GoldenEye",
            BackupType = N64BackupType.Eeprom,
            BackupSize = 512,
            RetraceByVsync = true,
            Rumble = true,
            UseTimer = false,
            RspMultiCore = true,
            ExpansionPak = false,
            Extra = "[Idle]\r\nCount = 1\r\nAddress0 = 0x800afc3c\r\nInst0 = 0x5443ffff\r\nType0 = 0",
        };

        var text = ini.ToText();
        var back = N64Ini.Parse(text);

        StringAssert.StartsWith(text, ";GoldenEye");
        StringAssert.Contains(text, "[RomOption]");
        StringAssert.Contains(text, "BackupType = 3");
        StringAssert.Contains(text, "BackupSize = 512");
        StringAssert.Contains(text, "RetraceByVsync = 1");
        StringAssert.Contains(text, "UseTimer = 0");
        StringAssert.Contains(text, "RSPMultiCore = 1");
        StringAssert.Contains(text, "RamSize = 0x400000");
        Assert.AreEqual("GoldenEye", back.Comment);
        Assert.AreEqual(N64BackupType.Eeprom, back.BackupType);
        Assert.AreEqual(512, back.BackupSize);
        Assert.AreEqual(true, back.RetraceByVsync);
        Assert.AreEqual(false, back.UseTimer);
        Assert.AreEqual(true, back.RspMultiCore);
        Assert.AreEqual(false, back.ExpansionPak);
        Assert.AreEqual(ini.Extra, back.Extra);
    }

    [TestMethod]
    public void ToText_NothingSet_IsJustTheSection()
    {
        Assert.AreEqual("[RomOption]" + Environment.NewLine, new N64Ini().ToText());
        Assert.AreEqual("[RomOption]" + Environment.NewLine + "RamSize = 0x800000" + Environment.NewLine, new N64Ini { ExpansionPak = true }.ToText());
    }

    [TestMethod]
    public void Parse_OddInput_IsLenient()
    {
        var ini = N64Ini.Parse("[RomOption]\nBackupType = 9\nRumble = maybe\nRamSize = 0x800000\n");

        Assert.IsNull(ini.BackupType, "out of range stays extra");
        Assert.IsNull(ini.Rumble);
        Assert.AreEqual(true, ini.ExpansionPak);
        StringAssert.Contains(ini.Extra, "BackupType = 9");
        StringAssert.Contains(ini.Extra, "Rumble = maybe");
        Assert.AreEqual("", N64Ini.Parse("").Extra);
        Assert.AreEqual("", N64Ini.Parse("[RomOption]\n").Extra);
        Assert.ThrowsExactly<ArgumentNullException>(() => N64Ini.Parse(null!));
    }
}
