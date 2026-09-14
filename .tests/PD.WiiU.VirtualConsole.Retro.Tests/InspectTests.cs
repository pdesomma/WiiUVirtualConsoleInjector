using System.IO.Compression;
using WiiUSharp.Rpx;

namespace PD.WiiU.VirtualConsole.Retro.Tests;

[TestClass]
public class InspectTests
{
    private string _root = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Retro.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_root, recursive: true);

    [TestMethod]
    public void Nes_MatchingRpx_PassesAndSnesRpxFails()
    {
        var nes = Title();
        RpxFile.Parse(FakeVcRpx.Build(nes: true)).Save(Path.Combine(nes.Code, "WUP-FAAE.rpx"), compress: true);
        var snes = Title();
        RpxFile.Parse(FakeVcRpx.Build(nes: false)).Save(Path.Combine(snes.Code, "WUP-JAAE.rpx"), compress: true);

        Assert.AreEqual(0, new NesRomInjector().Inspect(nes).Count);
        Assert.AreEqual(0, new SnesRomInjector().Inspect(snes).Count);
        var wrong = new NesRomInjector().Inspect(snes).Single();
        Assert.AreEqual("code/WUP-JAAE.rpx", wrong.Path);
        Assert.AreEqual("executable is a SNES title", wrong.Message);
        Assert.AreEqual("executable is a NES title", new SnesRomInjector().Inspect(nes).Single().Message);
    }

    [TestMethod]
    public void Nes_NoOrBrokenRpx_ReportsIt()
    {
        var empty = Title();
        var broken = Title();
        File.WriteAllBytes(Path.Combine(broken.Code, "WUP-FAAE.rpx"), new byte[64]);

        Assert.AreEqual("no *.rpx file", new NesRomInjector().Inspect(empty).Single().Message);
        Assert.AreEqual("code/WUP-FAAE.rpx", new NesRomInjector().Inspect(broken).Single().Path);
        Assert.ThrowsExactly<ArgumentNullException>(() => new NesRomInjector().Inspect(null!));
    }

    [TestMethod]
    public void N64_RomFolderAndLayout_Checked()
    {
        var good = Title();
        Directory.CreateDirectory(Path.Combine(good.Content, "rom"));
        File.WriteAllBytes(Path.Combine(good.Content, "rom", "NAAE.z64"), new byte[64]);
        File.WriteAllBytes(Path.Combine(good.Content, "FrameLayout.arc"), FakeFrameLayout.Build());
        var twoRoms = Title();
        Directory.CreateDirectory(Path.Combine(twoRoms.Content, "rom"));
        File.WriteAllBytes(Path.Combine(twoRoms.Content, "rom", "a.z64"), new byte[1]);
        File.WriteAllBytes(Path.Combine(twoRoms.Content, "rom", "b.z64"), new byte[1]);

        Assert.AreEqual(0, new N64RomInjector().Inspect(good).Count);
        CollectionAssert.AreEqual(new[] { "content/rom: expected exactly one ROM file", "content/FrameLayout.arc: file missing" }, new N64RomInjector().Inspect(twoRoms).Select(i => i.ToString()).ToArray());
        Assert.AreEqual("content/rom: folder missing", new N64RomInjector().Inspect(Title()).First().ToString());
    }

    [TestMethod]
    public void Tg16AndMsx_NeedTheirPackages()
    {
        var tg16 = Title();
        Directory.CreateDirectory(Path.Combine(tg16.Content, "pceemu"));
        File.WriteAllBytes(Path.Combine(tg16.Content, "pceemu", "pce.pkg"), new byte[8]);
        var msx = Title();
        Directory.CreateDirectory(Path.Combine(msx.Content, "msx"));
        File.WriteAllBytes(Path.Combine(msx.Content, "msx", "msx.pkg"), new byte[0x580B3]);
        var shortMsx = Title();
        Directory.CreateDirectory(Path.Combine(shortMsx.Content, "msx"));
        File.WriteAllBytes(Path.Combine(shortMsx.Content, "msx", "msx.pkg"), new byte[100]);

        Assert.AreEqual(0, new Tg16RomInjector().Inspect(tg16).Count);
        Assert.AreEqual("content/pceemu/pce.pkg: file missing", new Tg16RomInjector().Inspect(Title()).Single().ToString());
        Assert.AreEqual(0, new MsxRomInjector().Inspect(msx).Count);
        Assert.AreEqual("100 bytes, expected at least 360627", new MsxRomInjector().Inspect(shortMsx).Single().Message);
    }

    [TestMethod]
    public void Nds_ArchiveEntryAndConfiguration_Checked()
    {
        var good = Title();
        var data = Path.Combine(good.Content, "0010");
        Directory.CreateDirectory(data);
        using (var zip = ZipFile.Open(Path.Combine(data, "rom.zip"), ZipArchiveMode.Create))
            zip.CreateEntry("WUP-XYZ.nds");
        File.WriteAllText(Path.Combine(data, "configuration_cafe.json"), "{}");
        var noEntry = Title();
        Directory.CreateDirectory(Path.Combine(noEntry.Content, "0010"));
        using (var zip = ZipFile.Open(Path.Combine(noEntry.Content, "0010", "rom.zip"), ZipArchiveMode.Create))
            zip.CreateEntry("other.nds");

        Assert.AreEqual(0, new NdsRomInjector().Inspect(good).Count);
        CollectionAssert.AreEqual(
            new[] { "content/0010/rom.zip: rom.zip has no WUP-named entry.", "content/0010/configuration_cafe.json: file missing" },
            new NdsRomInjector().Inspect(noEntry).Select(i => i.ToString()).ToArray());
    }

    private TitleDirectory Title() => TitleDirectory.Create(Path.Combine(_root, Guid.NewGuid().ToString("N")));
}
