using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class ArtworkFramesTests
{
    [TestMethod]
    public void All_EveryFrame_HasAUniqueKeyAndArtSizedForItsSlot()
    {
        var keys = ArtworkFrames.All.Select(f => f.Key).ToArray();

        Assert.AreEqual(keys.Length, keys.Distinct().Count(), "keys repeat");
        foreach (var frame in ArtworkFrames.All)
        {
            Assert.IsTrue(frame.Slot == ImageSlot.BootTv || frame.Slot == ImageSlot.Icon || frame.Slot == ImageSlot.BootLogo, frame.Key + " belongs to no drawable slot");
            Assert.AreEqual(frame.Slot != ImageSlot.BootLogo, frame.Window is not null, frame.Key + " window");
        }
    }

    [TestMethod]
    public void For_Console_ListsItsOwnThenSharedThenPlainAndNothingElse()
    {
        var icons = ArtworkFrames.For(ImageSlot.Icon, SourceConsole.Nes);

        CollectionAssert.AreEqual(new[] { "icon-nes-1", "icon-nes-2", "icon-vc", "icon-plain" }, icons.Select(f => f.Key).ToArray());
        Assert.IsTrue(icons.All(f => f.Slot == ImageSlot.Icon));
    }

    [TestMethod]
    public void For_GamePad_SharesTheTvFrames()
    {
        CollectionAssert.AreEqual(
            ArtworkFrames.For(ImageSlot.BootTv, SourceConsole.Gba).ToArray(),
            ArtworkFrames.For(ImageSlot.BootDrc, SourceConsole.Gba).ToArray());
        Assert.IsTrue(ArtworkFrames.For(ImageSlot.BootDrc, SourceConsole.Gba).Any(f => f.Key == "gbc"), "Game Boy Color rides on the GBA base");
    }

    [TestMethod]
    public void For_Logo_IsTheSameForEveryConsole()
    {
        foreach (var console in (SourceConsole[])Enum.GetValues(typeof(SourceConsole)))
            CollectionAssert.AreEqual(new[] { "logo-pill", "logo-plain" }, ArtworkFrames.For(ImageSlot.BootLogo, console).Select(f => f.Key).ToArray(), console.ToString());
    }

    [TestMethod]
    public void For_EveryConsole_HasAtLeastOneRealTvAndIconFrame()
    {
        foreach (var console in (SourceConsole[])Enum.GetValues(typeof(SourceConsole)))
        {
            Assert.IsTrue(ArtworkFrames.For(ImageSlot.BootTv, console).Any(f => !f.IsPlain), console + " boot");
            Assert.IsTrue(ArtworkFrames.For(ImageSlot.Icon, console).Any(f => !f.IsPlain), console + " icon");
        }
    }

    [TestMethod]
    public void Find_KnownAndUnknown()
    {
        Assert.AreEqual("Super Famicom", ArtworkFrames.Find("snes-sfc")!.Name);
        Assert.IsNull(ArtworkFrames.Find("nope"));
        Assert.IsNull(ArtworkFrames.Find(null));
        Assert.ThrowsExactly<ArgumentNullException>(() => ArtworkFrames.For(null!, SourceConsole.Nes));
    }

    [TestMethod]
    public void DefaultWindow_NoOverlay_IsTheWholeImage()
    {
        Assert.AreEqual(ArtworkFrames.IconFull, ArtworkFrames.DefaultWindow(ImageSlot.Icon));
        Assert.AreEqual(ArtworkFrames.BootFull, ArtworkFrames.DefaultWindow(ImageSlot.BootTv));
        Assert.AreEqual(ArtworkFrames.BootFull, ArtworkFrames.DefaultWindow(ImageSlot.BootDrc));
        Assert.IsNull(ArtworkFrames.DefaultWindow(ImageSlot.BootLogo));
        Assert.AreEqual(ArtworkFrames.IconFull, ArtworkFrames.Find("icon-plain")!.Window);
        Assert.AreEqual(ArtworkFrames.BootFull, ArtworkFrames.Find("boot-plain")!.Window);
    }

    [TestMethod]
    public void ArtworkRequest_JapaneseName_CaptionsInJapanese()
    {
        var request = new ArtworkRequest(null) { NameLine1 = "スーパーメトロイド", ReleaseYear = 1994, Players = 1 };

        Assert.IsTrue(request.IsJapanese);
        Assert.AreEqual("1994年発売", request.ReleasedText);
        Assert.AreEqual("プレイ人数　1人", request.PlayersText);
    }

    [TestMethod]
    public void ArtworkRequest_EnglishName_CaptionsInEnglishWithPlayerRanges()
    {
        Assert.AreEqual("Players: 1", new ArtworkRequest(null) { NameLine1 = "Game", Players = 1 }.PlayersText);
        Assert.AreEqual("Players: 1-3", new ArtworkRequest(null) { Players = 3 }.PlayersText);
        Assert.AreEqual("Players: 1-4", new ArtworkRequest(null) { Players = 8 }.PlayersText);
        Assert.AreEqual("Released: 1985", new ArtworkRequest(null) { ReleaseYear = 1985 }.ReleasedText);
        Assert.IsNull(new ArtworkRequest(null).PlayersText);
        Assert.IsNull(new ArtworkRequest(null) { ReleaseYear = 0 }.ReleasedText);
    }
}
