using System.Text;

namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class RomNamesTests
{
    private string _root = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "PD.WiiU.VirtualConsole.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_root, recursive: true);

    [TestMethod]
    public void Suggest_DiscImages_ReadTheHeaderTitle()
    {
        var iso = Write("game.iso", 0x100, (0x20, "LUIGI'S MANSION"));
        var wbfs = Write("game.wbfs", 0x300, (0x220, "Super Paper Mario"));
        var gcz = Write("game.gcz", 0x100, (0x20, "Should Not Be Read"));

        Assert.AreEqual("Luigi's Mansion", RomNames.Suggest(SourceConsole.GameCube, iso), "a shouting header gets title case");
        Assert.AreEqual("Super Paper Mario", RomNames.Suggest(SourceConsole.Wii, wbfs));
        Assert.IsNull(RomNames.Suggest(SourceConsole.GameCube, gcz), "compressed images are not opened");
    }

    [TestMethod]
    public void Suggest_Cartridges_ReadEachHeaderLayout()
    {
        var gba = Write("game.gba", 0x100, (0xA0, "POKEMON EMER"));
        var gb = Write("game.gb", 0x200, (0x134, "BATMAN"));
        var nds = Write("game.nds", 0x100, (0, "SUPERMAN RET"));
        var z64 = Write("game.z64", 0x40, (0x20, "GOLDENEYE"));
        var bytes = File.ReadAllBytes(z64);
        bytes[0] = 0x80;
        File.WriteAllBytes(z64, bytes);
        var v64 = Path.Combine(_root, "game.v64");
        var swapped = (byte[])bytes.Clone();
        swapped[0] = 0x37;
        for (var i = 0x20; i < 0x34; i += 2)
            (swapped[i], swapped[i + 1]) = (swapped[i + 1], swapped[i]);
        File.WriteAllBytes(v64, swapped);
        var n64 = Path.Combine(_root, "game.n64");
        var wordSwapped = (byte[])bytes.Clone();
        wordSwapped[0] = 0x40;
        for (var i = 0x20; i < 0x34; i += 4)
            (wordSwapped[i], wordSwapped[i + 1], wordSwapped[i + 2], wordSwapped[i + 3]) = (wordSwapped[i + 3], wordSwapped[i + 2], wordSwapped[i + 1], wordSwapped[i]);
        File.WriteAllBytes(n64, wordSwapped);

        Assert.AreEqual("Pokemon Emer", RomNames.Suggest(SourceConsole.Gba, gba));
        Assert.AreEqual("Batman", RomNames.Suggest(SourceConsole.Gba, gb));
        Assert.AreEqual("Superman Ret", RomNames.Suggest(SourceConsole.Nds, nds), "no banner: the header code");
        var banner = new byte[0x1000];
        Encoding.ASCII.GetBytes("SMR-NDS").CopyTo(banner, 0);
        banner[0x68] = 0x00;
        banner[0x69] = 0x08;
        Encoding.Unicode.GetBytes("Superman Returns\u2122\nElectronic Arts Inc.").CopyTo(banner, 0x800 + 0x340);
        var withBanner = Path.Combine(_root, "banner.nds");
        File.WriteAllBytes(withBanner, banner);
        Assert.AreEqual("Superman Returns", RomNames.Suggest(SourceConsole.Nds, withBanner), "the banner's first line, trademark dropped");
        Assert.AreEqual("Goldeneye", RomNames.Suggest(SourceConsole.N64, z64));
        Assert.AreEqual("Goldeneye", RomNames.Suggest(SourceConsole.N64, v64), "byte-swapped dump");
        Assert.AreEqual("Goldeneye", RomNames.Suggest(SourceConsole.N64, n64), "word-swapped dump");
    }

    [TestMethod]
    public void Suggest_SuperNintendo_TakesTheHeaderWhoseChecksumChecksOut()
    {
        var lorom = Snes(0x7FC0, "ANIMANIACS", copierHeader: false);
        var hirom = Snes(0xFFC0, "SUPER METROID", copierHeader: true);
        var neither = Write("broken.sfc", 0x10000, (0x7FC0, "NOT VALID"));

        Assert.AreEqual("Animaniacs", RomNames.Suggest(SourceConsole.Snes, lorom));
        Assert.AreEqual("Super Metroid", RomNames.Suggest(SourceConsole.Snes, hirom), "past the 512-byte copier header");
        Assert.IsNull(RomNames.Suggest(SourceConsole.Snes, neither), "no checksum, no trust");
    }

    [TestMethod]
    public void Suggest_NothingToRead_IsNull()
    {
        var nes = Write("game.nes", 0x100, (0, "NES\x1a"));
        var binary = Path.Combine(_root, "junk.nds");
        File.WriteAllBytes(binary, Enumerable.Range(0, 0x100).Select(i => (byte)(0x80 + i)).ToArray());

        Assert.IsNull(RomNames.Suggest(SourceConsole.Nes, nes));
        Assert.IsNull(RomNames.Suggest(SourceConsole.Msx, nes));
        Assert.IsNull(RomNames.Suggest(SourceConsole.Nds, binary), "not printable");
        Assert.IsNull(RomNames.Suggest(SourceConsole.Wii, Path.Combine(_root, "missing.iso")));
        Assert.IsNull(RomNames.Suggest(SourceConsole.Wii, Write("short.iso", 0x30, (0x20, "X"))), "too short for the field");
        Assert.ThrowsExactly<ArgumentNullException>(() => RomNames.Suggest(SourceConsole.Wii, null!));
    }

    [TestMethod]
    public void Tidy_TrimsCollapsesAndTitleCasesShouting()
    {
        Assert.AreEqual("Mario Kart Wii", RomNames.Tidy("  MARIO   KART WII  "));
        Assert.AreEqual("Mario Kart Wii", RomNames.Tidy("Mario Kart Wii"), "mixed case stays");
        Assert.AreEqual("F-Zero Gx", RomNames.Tidy("F-ZERO GX"));
        Assert.IsNull(RomNames.Tidy("   "));
        Assert.IsNull(RomNames.Tidy(null));
    }

    private string Snes(int header, string name, bool copierHeader)
    {
        var skip = copierHeader ? 512 : 0;
        var bytes = new byte[0x10000 + skip];
        var at = skip + header;
        Encoding.ASCII.GetBytes(name.PadRight(21)).CopyTo(bytes, at);
        bytes[at + 0x1C] = 0x34;
        bytes[at + 0x1D] = 0x12;
        bytes[at + 0x1E] = 0xCB;
        bytes[at + 0x1F] = 0xED;
        var path = Path.Combine(_root, name.Replace(' ', '_') + ".sfc");
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private string Write(string name, int length, (int Offset, string Text) field)
    {
        var bytes = new byte[length];
        Encoding.ASCII.GetBytes(field.Text).CopyTo(bytes, field.Offset);
        var path = Path.Combine(_root, name);
        File.WriteAllBytes(path, bytes);
        return path;
    }
}
