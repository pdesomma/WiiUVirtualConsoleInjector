namespace PD.WiiU.VirtualConsole.Wii.Tests;

[TestClass]
public class NintendontConfigTests
{
    [TestMethod]
    public void ToBytes_Defaults_HasMagicVersionSizeAndForwarderFlags()
    {
        var bytes = new NintendontConfig().ToBytes();

        Assert.AreEqual(NintendontConfig.Size, bytes.Length);
        Assert.AreEqual(0x01070CF6u, Word(bytes, 0));
        Assert.AreEqual(0x0000000Au, Word(bytes, 4));
        Assert.AreEqual(1u << 3 | 1u << 7, Word(bytes, 8), "memory card emulation and autoboot");
        Assert.AreEqual(0u, Word(bytes, 12), "auto video");
        Assert.AreEqual(unchecked((uint)-1), Word(bytes, 16), "auto language");
        Assert.AreEqual(4u, Word(bytes, 532));
        Assert.AreEqual(0u, Word(bytes, 536));
        Assert.AreEqual(2, bytes[540]);
        Assert.AreEqual(0u, Word(bytes, 544));
    }

    [TestMethod]
    public void ToBytes_EverythingSet_PacksBitsBigEndian()
    {
        var config = new NintendontConfig
        {
            Cheats = true,
            CheatPath = "/codes/GALE01.gct",
            ForceWidescreen = true,
            ForceProgressive = true,
            RemoveLimit = true,
            Log = true,
            MemoryCardShared = true,
            NativeControl = true,
            WiiUWidescreen = true,
            ArcadeMode = true,
            ClassicControllerRumble = true,
            SkipIpl = true,
            BroadbandEmulation = true,
            MemoryCardEmulation = false,
            AutoBoot = false,
            Video = NintendontVideo.ForceDeflicker,
            ForcedMode = NintendontForcedMode.Pal60,
            Progressive = true,
            PatchPal50 = true,
            Language = NintendontLanguage.German,
            GamePath = "/games/Melee/game.iso",
            Pads = 1,
            MemoryCardSize = 5,
            VideoScale = -3,
            VideoShift = 7,
            NetworkProfile = 2,
            GamePadSlot = 3,
        };

        var bytes = config.ToBytes();

        Assert.AreEqual(1u | 1u << 4 | 1u << 5 | 1u << 6 | 1u << 8 | 1u << 12 | 1u << 13 | 1u << 14 | 1u << 15 | 1u << 16 | 1u << 17 | 1u << 18 | 1u << 19, Word(bytes, 8));
        Assert.AreEqual(4u << 16 | 0x2 | 0x10 | 0x20, Word(bytes, 12));
        Assert.AreEqual(1u, Word(bytes, 16));
        Assert.AreEqual("/games/Melee/game.iso", Text(bytes, 20));
        Assert.AreEqual("/codes/GALE01.gct", Text(bytes, 275));
        Assert.AreEqual(1u, Word(bytes, 532));
        Assert.AreEqual(5, bytes[540]);
        Assert.AreEqual(-3, (sbyte)bytes[541]);
        Assert.AreEqual(7, (sbyte)bytes[542]);
        Assert.AreEqual(2, bytes[543]);
        Assert.AreEqual(3u, Word(bytes, 544));
    }

    [TestMethod]
    public void Parse_WrittenBytes_RoundTrips()
    {
        var config = new NintendontConfig
        {
            Cheats = true,
            Video = NintendontVideo.Force,
            ForcedMode = NintendontForcedMode.MPal,
            Language = NintendontLanguage.Italian,
            Pads = 2,
            MemoryCardSize = 0,
            MemoryCardShared = true,
            SkipIpl = true,
            VideoShift = -5,
            NetworkProfile = 1,
            GamePadSlot = 2,
        };

        var back = NintendontConfig.Parse(config.ToBytes());

        Assert.IsTrue(back.Cheats);
        Assert.AreEqual(NintendontVideo.Force, back.Video);
        Assert.AreEqual(NintendontForcedMode.MPal, back.ForcedMode);
        Assert.AreEqual(NintendontLanguage.Italian, back.Language);
        Assert.AreEqual(2u, back.Pads);
        Assert.AreEqual(0, back.MemoryCardSize);
        Assert.IsTrue(back.MemoryCardShared);
        Assert.IsTrue(back.SkipIpl);
        Assert.IsTrue(back.MemoryCardEmulation);
        Assert.IsTrue(back.AutoBoot);
        Assert.AreEqual(-5, back.VideoShift);
        Assert.AreEqual(1, back.NetworkProfile);
        Assert.AreEqual(2u, back.GamePadSlot);
        CollectionAssert.AreEqual(config.ToBytes(), back.ToBytes());
    }

    [TestMethod]
    public void Parse_OlderShorterFile_KeepsDefaultsForMissingTail()
    {
        var bytes = new NintendontConfig { GamePadSlot = 3, VideoScale = 9 }.ToBytes().Take(541).ToArray();

        var config = NintendontConfig.Parse(bytes);

        Assert.AreEqual(0u, config.GamePadSlot);
        Assert.AreEqual(0, config.VideoScale);
        Assert.AreEqual(2, config.MemoryCardSize);
    }

    [TestMethod]
    public void Parse_BadInput_Throws()
    {
        var wrongMagic = new NintendontConfig().ToBytes();
        wrongMagic[0] = 0;

        Assert.ThrowsExactly<ArgumentNullException>(() => NintendontConfig.Parse(null!));
        Assert.ThrowsExactly<InvalidDataException>(() => NintendontConfig.Parse(wrongMagic));
        Assert.ThrowsExactly<InvalidDataException>(() => NintendontConfig.Parse(new byte[100]));
    }

    [TestMethod]
    public void ToBytes_OutOfRange_Throws()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => new NintendontConfig { Pads = 5 }.ToBytes());
        Assert.ThrowsExactly<InvalidOperationException>(() => new NintendontConfig { MemoryCardSize = 6 }.ToBytes());
    }

    [TestMethod]
    public void MemoryCardBlocks_EachIndex_MatchesNintendont()
    {
        CollectionAssert.AreEqual(new[] { 59, 123, 251, 507, 1019, 2043 }, Enumerable.Range(0, 6).Select(NintendontConfig.MemoryCardBlocks).ToArray());
    }

    private static string Text(byte[] bytes, int offset)
    {
        var end = Array.IndexOf(bytes, (byte)0, offset);
        return System.Text.Encoding.ASCII.GetString(bytes, offset, end - offset);
    }

    private static uint Word(byte[] bytes, int offset) => (uint)(bytes[offset] << 24 | bytes[offset + 1] << 16 | bytes[offset + 2] << 8 | bytes[offset + 3]);
}
