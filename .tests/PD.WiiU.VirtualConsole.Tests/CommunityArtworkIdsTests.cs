using System.Text;

namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class CommunityArtworkIdsTests
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
    public void LettersFor_KnownNumbers_MatchThePreviousApplication()
    {
        Assert.AreEqual("HLPT", CommunityArtworkIds.LettersFor(123456789));
        Assert.AreEqual("VRNJ", CommunityArtworkIds.LettersFor(987654321));
        Assert.AreEqual("FEEC", CommunityArtworkIds.LettersFor(1000000), "seven digits: the last letter comes from one character");
        Assert.AreEqual("HLPT", CommunityArtworkIds.LettersFor(12345678));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => CommunityArtworkIds.LettersFor(999999));
    }

    [TestMethod]
    public void Candidates_HeaderCodes_ComeWithRegionVariants()
    {
        var gba = Write("game.gba", 0x100, (0xAC, "AGBE"));
        var nds = Write("game.nds", 0x200, (0xC, "ARPE"));
        var iso = Write("game.iso", 0x100, (0, "RSPE01"));
        var wbfs = Write("game.wbfs", 0x300, (0x200, "RSPP01"));

        CollectionAssert.AreEqual(new[] { "gba/AGBE", "gba/AGBP", "gba/AGBJ" }, CommunityArtworkIds.Candidates(SourceConsole.Gba, gba).ToArray(), "the region variant equal to the code is not repeated");
        CollectionAssert.AreEqual(new[] { "nds/ARPE", "nds/ARPP", "nds/ARPJ" }, CommunityArtworkIds.Candidates(SourceConsole.Nds, nds).ToArray());
        CollectionAssert.AreEqual(new[] { "wii/RSPE01", "wii/RSPP01", "wii/RSPJ01" }, CommunityArtworkIds.Candidates(SourceConsole.Wii, iso).ToArray());
        CollectionAssert.AreEqual(new[] { "gcn/RSPP01", "gcn/RSPE01", "gcn/RSPJ01" }, CommunityArtworkIds.Candidates(SourceConsole.GameCube, wbfs).ToArray(), "WBFS keeps the header at 0x200");
    }

    [TestMethod]
    public void Candidates_N64_AddsTheByteSwappedCode()
    {
        var v64 = Write("game.v64", 0x100, (0x3A, "\0NEGE\0"));

        CollectionAssert.AreEqual(new[] { "n64/NEGE", "n64/NGEE" }, CommunityArtworkIds.Candidates(SourceConsole.N64, v64).ToArray());
    }

    [TestMethod]
    public void Candidates_HashedConsoles_MatchThePreviousApplication()
    {
        var nes = Path.Combine(_root, "game.nes");
        File.WriteAllBytes(nes, Enumerable.Range(0, 0x8100).Select(i => (byte)(i * 7)).ToArray());
        var head = Enumerable.Range(0, 0x210).Select(i => (byte)(i * 13)).ToArray();
        var msx = Path.Combine(_root, "game.rom");
        File.WriteAllBytes(msx, head);
        var tg16 = Path.Combine(_root, "game.pce");
        File.WriteAllBytes(tg16, head);
        var snesWithCode = Write("code.sfc", 0x10000, (0x7FB2, "ABCD"));
        var snesHiRom = Write("hirom.sfc", 0x10000, (0xFFC0, "SUPER TEST GAME      "));

        CollectionAssert.AreEqual(new[] { "nes/HIMJ" }, CommunityArtworkIds.Candidates(SourceConsole.Nes, nes).ToArray());
        CollectionAssert.AreEqual(new[] { "msx/LHTKSX" }, CommunityArtworkIds.Candidates(SourceConsole.Msx, msx).ToArray());
        CollectionAssert.AreEqual(new[] { "tg16/LHTKTG" }, CommunityArtworkIds.Candidates(SourceConsole.Tg16, tg16).ToArray());
        CollectionAssert.AreEqual(new[] { "snes/JVMG" }, CommunityArtworkIds.Candidates(SourceConsole.Snes, snesWithCode).ToArray(), "a LoROM code leaves the name empty, as before");
        CollectionAssert.AreEqual(new[] { "snes/JMJM" }, CommunityArtworkIds.Candidates(SourceConsole.Snes, snesHiRom).ToArray());
    }

    [TestMethod]
    public void Candidates_Genesis_IsEmptyWithoutAFolder()
    {
        var genesis = Write("game.md", 0x200, (0x100, "SEGA MEGA DRIVE "));

        Assert.AreEqual(0, CommunityArtworkIds.Candidates(SourceConsole.Genesis, genesis).Count, "no repository folder, so nothing to look up");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => CommunityArtworkIds.Folder(SourceConsole.Genesis));
    }

    [TestMethod]
    public void Candidates_NothingToLookUpBy_IsEmpty()
    {
        var tiny = Path.Combine(_root, "tiny.nes");
        File.WriteAllBytes(tiny, new byte[0x100]);
        var gb = Write("game.gb", 0x100, (0xAC, "AGBE"));
        var blank = Path.Combine(_root, "blank.nds");
        File.WriteAllBytes(blank, new byte[0x200]);

        Assert.AreEqual(0, CommunityArtworkIds.Candidates(SourceConsole.Nes, tiny).Count, "too short to hash");
        Assert.AreEqual(0, CommunityArtworkIds.Candidates(SourceConsole.Gba, gb).Count, "Game Boy ROMs carry no code");
        Assert.AreEqual(0, CommunityArtworkIds.Candidates(SourceConsole.Nds, blank).Count, "an all-zero code is nothing");
        Assert.ThrowsExactly<ArgumentNullException>(() => CommunityArtworkIds.Candidates(SourceConsole.Nes, null!));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => CommunityArtworkIds.Folder((SourceConsole)99));
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
