namespace PD.WiiU.VirtualConsole.Tests;

[TestClass]
public class ArtworkTests
{
    [TestMethod]
    public void NoneHasNoPaths()
    {
        foreach (var slot in ImageSlot.All)
            Assert.IsNull(Artwork.None.PathFor(slot));
    }

    [TestMethod]
    public void PathForReturnsTheMatchingSlot()
    {
        var artwork = new Artwork
        {
            Icon = "icon.png",
            BootTv = "tv.png",
            BootDrc = "drc.png",
            BootLogo = "logo.png",
        };

        Assert.AreEqual("icon.png", artwork.PathFor(ImageSlot.Icon));
        Assert.AreEqual("tv.png", artwork.PathFor(ImageSlot.BootTv));
        Assert.AreEqual("drc.png", artwork.PathFor(ImageSlot.BootDrc));
        Assert.AreEqual("logo.png", artwork.PathFor(ImageSlot.BootLogo));
    }
}
