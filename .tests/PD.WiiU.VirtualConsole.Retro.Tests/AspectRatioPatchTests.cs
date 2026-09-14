using WiiUSharp.Rpx;

namespace PD.WiiU.VirtualConsole.Retro.Tests;

[TestClass]
public class AspectRatioPatchTests
{
    [TestMethod]
    public void Apply_NesPixelPerfect_RewritesFirstTvAndLastPadMatch()
    {
        var rpx = RpxFile.Parse(FakeVcRpx.Build(nes: true));

        var applied = AspectRatioPatch.Apply(rpx, AspectRatio.PixelPerfect, nes: true);

        Assert.IsTrue(applied);
        var text = rpx.FindSection(".text")!.Data;
        CollectionAssert.AreEqual(new byte[] { 0x04, 0x38, 0x38, 0xE0, 0x09, 0x5F, 0x90 }, text.Skip(0x100).Take(7).ToArray());
        CollectionAssert.AreEqual(FakeVcRpx.PadPattern, text.Skip(0x200).Take(6).ToArray(), "earlier pad match is left alone");
        CollectionAssert.AreEqual(new byte[] { 0x39, 0x20, 0x04, 0x29, 0x38, 0x60 }, text.Skip(0x2800).Take(6).ToArray());
    }

    [TestMethod]
    public void Apply_SnesPixelPerfect_UsesSnesValues()
    {
        var rpx = RpxFile.Parse(FakeVcRpx.Build(nes: false));

        AspectRatioPatch.Apply(rpx, AspectRatio.PixelPerfect, nes: false);

        var text = rpx.FindSection(".text")!.Data;
        CollectionAssert.AreEqual(new byte[] { 0x04, 0x38, 0x38, 0xE0, 0x08, 0xC0, 0x90 }, text.Skip(0x100).Take(7).ToArray());
        CollectionAssert.AreEqual(new byte[] { 0x39, 0x20, 0x03, 0xE4, 0x38, 0x60 }, text.Skip(0x2800).Take(6).ToArray());
    }

    [TestMethod]
    public void Apply_Wide_IgnoresConsole()
    {
        var rpx = RpxFile.Parse(FakeVcRpx.Build(nes: true));

        AspectRatioPatch.Apply(rpx, AspectRatio.Wide, nes: true);

        var text = rpx.FindSection(".text")!.Data;
        CollectionAssert.AreEqual(new byte[] { 0x04, 0x38, 0x38, 0xE0, 0x05, 0xA0, 0x90 }, text.Skip(0x100).Take(7).ToArray());
        CollectionAssert.AreEqual(new byte[] { 0x39, 0x20, 0x02, 0x80, 0x38, 0x60 }, text.Skip(0x2800).Take(6).ToArray());
    }

    [TestMethod]
    public void Apply_PatternsMissing_ReturnsFalseAndChangesNothing()
    {
        var rpx = RpxFile.Parse(FakeVcRpx.Build(nes: true));
        var text = rpx.FindSection(".text")!;
        var scrubbed = text.Data;
        scrubbed[0x100] = 0;
        text.Data = scrubbed;

        Assert.IsFalse(AspectRatioPatch.Apply(rpx, AspectRatio.PixelPerfect, nes: true));
        CollectionAssert.AreEqual(FakeVcRpx.PadPattern, text.Data.Skip(0x2800).Take(6).ToArray());
    }

    [TestMethod]
    public void Apply_NullRpx_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => AspectRatioPatch.Apply(null!, AspectRatio.Wide, nes: false));
    }
}
