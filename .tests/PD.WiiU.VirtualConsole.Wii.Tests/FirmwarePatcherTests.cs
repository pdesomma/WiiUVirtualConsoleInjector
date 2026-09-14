using System.Text;
using PD.WiiU.VirtualConsole.Options;

namespace PD.WiiU.VirtualConsole.Wii.Tests;

[TestClass]
public class FirmwarePatcherTests
{
    [TestMethod]
    public void AppliesEveryFakeSignSite()
    {
        var image = Image((0x100, new byte[] { 0x20, 0x07, 0x23, 0xA2 }), (0x200, new byte[] { 0x20, 0x07, 0x4B, 0x0B }));

        var results = FirmwarePatcher.Apply(image, new[] { FirmwarePatch.FakeSign });

        Assert.AreEqual(new FirmwarePatchResult(FirmwarePatch.FakeSign, 2), results.Single());
        CollectionAssert.AreEqual(new byte[] { 0x20, 0x00, 0x23, 0xA2 }, image.Skip(0x100).Take(4).ToArray());
        CollectionAssert.AreEqual(new byte[] { 0x20, 0x00, 0x4B, 0x0B }, image.Skip(0x200).Take(4).ToArray());
    }

    [TestMethod]
    public void DistinctPatchesAreAppliedOnce()
    {
        var image = Image((0x100, new byte[] { 0x20, 0x07, 0x23, 0xA2 }));

        var results = FirmwarePatcher.Apply(image, new[] { FirmwarePatch.FakeSign, FirmwarePatch.FakeSign });

        Assert.AreEqual(1, results.Count);
    }

    [TestMethod]
    public void HomebrewConditionalPatchNeedsBothHalves()
    {
        var head = new byte[] { 0x0D, 0x80, 0x00, 0x00, 0x0D, 0x80, 0x00, 0x00 };
        var yes = Image((0x100, head), (0x110, new byte[] { 0x00, 0x00, 0x00, 0x02 }));
        var no = Image((0x100, head), (0x110, new byte[] { 0x00, 0x00, 0x00, 0x09 }));

        var yesResult = FirmwarePatcher.Apply(yes, new[] { FirmwarePatch.Homebrew }).Single();
        var noResult = FirmwarePatcher.Apply(no, new[] { FirmwarePatch.Homebrew }).Single();

        Assert.AreEqual(1, yesResult.Matches);
        CollectionAssert.AreEqual(new byte[] { 0x00, 0x00, 0x00, 0x03 }, yes.Skip(0x110).Take(4).ToArray());
        Assert.AreEqual(0, noResult.Matches);
        Assert.AreEqual(0x09, no[0x113]);
    }

    [TestMethod]
    public void HomebrewNintendontPatchWritesBeforeTheMatch()
    {
        var image = Image((0x100, new byte[] { 0xB0, 0xBA, 0x1C, 0x0F }));

        FirmwarePatcher.Apply(image, new[] { FirmwarePatch.Homebrew });

        CollectionAssert.AreEqual(
            new byte[] { 0xE5, 0x9F, 0x10, 0x04, 0xE5, 0x91, 0x00, 0x00, 0xE1, 0x2F, 0xFF, 0x10, 0x12, 0xFF, 0xFF, 0xE0 },
            image.Skip(0x100 - 12).Take(16).ToArray());
    }

    [TestMethod]
    public void HorizontalRemapScattersEightBytes()
    {
        var image = Image((0x100, new byte[] { 0x4A, 0x71, 0x42, 0x13, 0xD0, 0xD2, 0x9B, 0x00 }));

        var result = FirmwarePatcher.Apply(image, new[] { FirmwarePatch.HorizontalWiiRemote }).Single();

        Assert.AreEqual(1, result.Matches);
        Assert.AreEqual(0x02, image[0x107]);
        Assert.AreEqual(0x03, image[0x10F]);
        Assert.AreEqual(0x01, image[0x11D]);
        Assert.AreEqual(0x00, image[0x12B]);
        Assert.AreEqual(0x07, image[0x165]);
        Assert.AreEqual(0x06, image[0x175]);
        Assert.AreEqual(0x04, image[0x185]);
        Assert.AreEqual(0x05, image[0x195]);
    }

    [TestMethod]
    public void InstantAndNoClassicControllerRewriteTheTail()
    {
        var pattern = new byte[] { 0x78, 0x93, 0x21, 0x10, 0x2B, 0x02, 0xD1, 0xB7 };
        var instant = Image((0x100, pattern));
        var none = Image((0x100, pattern));

        FirmwarePatcher.Apply(instant, new[] { FirmwarePatch.InstantClassicController });
        FirmwarePatcher.Apply(none, new[] { FirmwarePatch.NoClassicController });

        CollectionAssert.AreEqual(new byte[] { 0x78, 0x93, 0x21, 0x10, 0x2B, 0x02, 0x46, 0xC0 }, instant.Skip(0x100).Take(8).ToArray());
        CollectionAssert.AreEqual(new byte[] { 0x78, 0x93, 0x21, 0x10, 0x2B, 0x02, 0xE0, 0xB7 }, none.Skip(0x100).Take(8).ToArray());
    }

    [TestMethod]
    public void PatchesForFollowsTheOptions()
    {
        CollectionAssert.AreEqual(
            new[] { FirmwarePatch.FakeSign, FirmwarePatch.Passthrough },
            FirmwarePatcher.PatchesFor(new WiiOptions()).ToArray());

        CollectionAssert.AreEqual(
            new[] { FirmwarePatch.FakeSign, FirmwarePatch.WiiRemote, FirmwarePatch.HorizontalWiiRemote, FirmwarePatch.ShoulderToTrigger, FirmwarePatch.Homebrew },
            FirmwarePatcher.PatchesFor(new WiiOptions { ControllerMode = WiiControllerMode.HorizontalWiiRemote, LrPatch = true, Passthrough = false }, homebrew: true).ToArray());

        CollectionAssert.AreEqual(
            new[] { FirmwarePatch.FakeSign, FirmwarePatch.NoClassicController },
            FirmwarePatcher.PatchesFor(new WiiOptions { ControllerMode = WiiControllerMode.NoClassicController, Passthrough = false }).ToArray());
    }

    [TestMethod]
    public void PatchFileRewritesInPlace()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".img");
        File.WriteAllBytes(path, Image((0x40, new byte[] { 0x16, 0x13, 0x1C, 0x02, 0x40, 0x9A, 0x1C, 0x13 })));
        try
        {
            var results = FirmwarePatcher.PatchFile(path, new[] { FirmwarePatch.WiiRemote });

            Assert.AreEqual(1, results.Single().Matches);
            CollectionAssert.AreEqual(new byte[] { 0x23, 0x00, 0x1C, 0x02 }, File.ReadAllBytes(path).Skip(0x40).Take(4).ToArray());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void ReadRevisionFindsTheMarker()
    {
        Assert.AreEqual("r590", FirmwarePatcher.ReadRevision(Image((0x300, Encoding.ASCII.GetBytes("svn-r590")))));
        Assert.IsNull(FirmwarePatcher.ReadRevision(Image()));
    }

    [TestMethod]
    public void ReportsZeroMatchesForAnUnrecognisedImage()
    {
        var image = Image();

        var results = FirmwarePatcher.Apply(image, new[] { FirmwarePatch.FakeSign, FirmwarePatch.ShoulderToTrigger });

        Assert.IsTrue(results.All(r => r.Matches == 0));
    }

    [TestMethod]
    public void ShoulderToTriggerAppliesAllFivePatterns()
    {
        var image = Image(
            (0x100, new byte[] { 0x40, 0x05, 0x46, 0xA9 }),
            (0x200, new byte[] { 0x1C, 0x05, 0x40, 0x35 }),
            (0x300, new byte[] { 0x23, 0x7F, 0x1C, 0x02 }),
            (0x400, new byte[] { 0x46, 0x53, 0x42, 0x18 }),
            (0x500, new byte[] { 0x1C, 0x05, 0x80, 0x22 }));

        var result = FirmwarePatcher.Apply(image, new[] { FirmwarePatch.ShoulderToTrigger }).Single();

        Assert.AreEqual(5, result.Matches);
        CollectionAssert.AreEqual(new byte[] { 0x46, 0xB1, 0x23, 0x20, 0x40, 0x03 }, image.Skip(0x300).Take(6).ToArray());
        CollectionAssert.AreEqual(new byte[] { 0x25, 0x40, 0x80, 0x22, 0x40, 0x05 }, image.Skip(0x500).Take(6).ToArray());
    }

    private static byte[] Image(params (int Offset, byte[] Bytes)[] contents)
    {
        var image = new byte[0x1000];
        for (var i = 0; i < image.Length; i++)
            image[i] = 0xEE;
        foreach (var (offset, bytes) in contents)
            bytes.CopyTo(image, offset);
        return image;
    }
}
