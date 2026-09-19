namespace PD.WiiU.VirtualConsole.RetroArch.Tests;

[TestClass]
public class DiscReferencesTests
{
    private string _root = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = TestPaths.TempRoot();
        Directory.CreateDirectory(_root);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [TestMethod]
    public void Flatten_CueWithFolderedFileLines_RewritesOnlyThePathAndKeepsCrlf()
    {
        var text = "REM COMMENT \"sub/keep me.txt\"\r\n  file \"subdir/Track 01.bin\" BINARY\r\nFILE ..\\Track02.bin BINARY\r\n  TRACK 01 MODE2/2352\r\n    INDEX 01 00:00:00\r\n";

        var flat = DiscReferences.Flatten(text, ".cue");

        Assert.AreEqual("REM COMMENT \"sub/keep me.txt\"\r\n  file \"Track 01.bin\" BINARY\r\nFILE Track02.bin BINARY\r\n  TRACK 01 MODE2/2352\r\n    INDEX 01 00:00:00\r\n", flat);
    }

    [TestMethod]
    public void Flatten_FlatCue_IsUnchanged()
    {
        var text = "FILE \"game.bin\" BINARY\n  TRACK 01 MODE2/2352\n";

        Assert.AreEqual(text, DiscReferences.Flatten(text, ".CUE"));
    }

    [TestMethod]
    public void Flatten_M3uEntries_KeepsCommentsBlanksAndWhitespace()
    {
        var text = "#EXTM3U\r\n\r\ndiscs\\Game (Disc 1).cue\r\n  /abs/Game (Disc 2).cue  \r\n";

        var flat = DiscReferences.Flatten(text, ".m3u");

        Assert.AreEqual("#EXTM3U\r\n\r\nGame (Disc 1).cue\r\n  Game (Disc 2).cue  \r\n", flat);
    }

    [TestMethod]
    public void Flatten_NullText_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => DiscReferences.Flatten(null!, ".cue"));
    }

    [TestMethod]
    public void Flatten_OtherExtension_IsUnchanged()
    {
        var text = "FILE \"sub/x.bin\" BINARY\n";

        Assert.AreEqual(text, DiscReferences.Flatten(text, ".chd"));
        Assert.AreEqual(text, DiscReferences.Flatten(text, ""));
    }

    [TestMethod]
    public void Of_BlankPath_ThrowsArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => DiscReferences.Of(" "));
        Assert.ThrowsExactly<ArgumentException>(() => DiscReferences.Of(null!));
    }

    [TestMethod]
    public void Of_CueWithQuotedAndBareNames_ResolvesBesideTheCue()
    {
        Write("Track 01.bin", "a");
        Write("track02.bin", "b");
        var cue = Write("game.cue", "FILE \"Track 01.bin\" BINARY\n  TRACK 01 MODE2/2352\n    INDEX 01 00:00:00\nfile track02.bin BINARY\n  TRACK 02 AUDIO\n    INDEX 01 00:00:00\n");

        var references = DiscReferences.Of(cue);

        CollectionAssert.AreEqual(new[] { Path.Combine(_root, "Track 01.bin"), Path.Combine(_root, "track02.bin") }, references.ToArray());
    }

    [TestMethod]
    public void Of_CueRepeatingAFile_ListsItOnce()
    {
        Write("game.bin", "a");
        var cue = Write("game.cue", "FILE \"game.bin\" BINARY\n  TRACK 01 MODE1/2352\nFILE \"GAME.BIN\" BINARY\n  TRACK 02 AUDIO\n");

        Assert.AreEqual(1, DiscReferences.Of(cue).Count);
    }

    [TestMethod]
    public void Of_CueWithSubfolderName_ResolvesIntoIt()
    {
        Directory.CreateDirectory(Path.Combine(_root, "subdir"));
        Write(Path.Combine("subdir", "track.bin"), "a");
        var cue = Write("game.cue", "FILE \"subdir/track.bin\" BINARY\n");

        CollectionAssert.AreEqual(new[] { Path.Combine(_root, "subdir", "track.bin") }, DiscReferences.Of(cue).ToArray());
    }

    [TestMethod]
    public void Of_M3uOfTwoCues_ListsCuesThenTheirBins()
    {
        Write("Game (Disc 1) (Track 1).bin", "1");
        Write("Game (Disc 1) (Track 2).bin", "2");
        Write("Game (Disc 2) (Track 1).bin", "3");
        Write("Game (Disc 2) (Track 2).bin", "4");
        Write("Game (Disc 1).cue", "FILE \"Game (Disc 1) (Track 1).bin\" BINARY\n  TRACK 01 MODE2/2352\nFILE \"Game (Disc 1) (Track 2).bin\" BINARY\n  TRACK 02 AUDIO\n");
        Write("Game (Disc 2).cue", "FILE \"Game (Disc 2) (Track 1).bin\" BINARY\n  TRACK 01 MODE2/2352\nFILE \"Game (Disc 2) (Track 2).bin\" BINARY\n  TRACK 02 AUDIO\n");
        var m3u = Write("Game.m3u", "#EXTM3U\r\nGame (Disc 1).cue\r\n\r\nGame (Disc 2).cue\r\n");

        var references = DiscReferences.Of(m3u).Select(p => Path.GetFileName(p)).ToArray();

        CollectionAssert.AreEqual(
            new[] { "Game (Disc 1).cue", "Game (Disc 1) (Track 1).bin", "Game (Disc 1) (Track 2).bin", "Game (Disc 2).cue", "Game (Disc 2) (Track 1).bin", "Game (Disc 2) (Track 2).bin" },
            references);
    }

    [TestMethod]
    public void Of_MissingReferencedFile_ThrowsFileNotFoundException()
    {
        var cue = Write("game.cue", "FILE \"nope.bin\" BINARY\n");

        var error = Assert.ThrowsExactly<FileNotFoundException>(() => DiscReferences.Of(cue));

        StringAssert.Contains(error.Message, "nope.bin");
        Assert.AreEqual(Path.Combine(_root, "nope.bin"), error.FileName);
    }

    [TestMethod]
    public void Of_NonReferringExtension_IsEmpty()
    {
        var chd = Write("game.chd", "not a playlist");

        Assert.AreEqual(0, DiscReferences.Of(chd).Count);
        Assert.AreEqual(0, DiscReferences.Of(Path.Combine(_root, "absent.pbp")).Count, "nothing is read");
    }

    [TestMethod]
    public void Refers_CueAndM3u_TrueOthersFalse()
    {
        Assert.IsTrue(DiscReferences.Refers(@"C:\x\game.CUE"));
        Assert.IsTrue(DiscReferences.Refers("game.m3u"));
        Assert.IsFalse(DiscReferences.Refers("game.chd"));
        Assert.IsFalse(DiscReferences.Refers("game"));
        Assert.ThrowsExactly<ArgumentNullException>(() => DiscReferences.Refers(null!));
    }

    private string Write(string name, string text)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllText(path, text);
        return path;
    }
}
