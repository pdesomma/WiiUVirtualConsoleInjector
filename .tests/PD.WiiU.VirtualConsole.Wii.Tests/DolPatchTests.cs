using PD.WiiU.VirtualConsole.Options;

namespace PD.WiiU.VirtualConsole.Wii.Tests;

[TestClass]
public class DolPatchTests
{
    [TestMethod]
    public void RemoveDeflicker_PatternPresent_RewritesOnlyTheFinalBranch()
    {
        var dol = FakeRetailDisc.Dol();
        var before = (byte[])dol.Clone();
        var end = FakeRetailDisc.DeflickerOffset + DolFilterPatches.DeflickerPattern.Length;

        Assert.IsTrue(DolFilterPatches.RemoveDeflicker(dol));

        CollectionAssert.AreEqual(new byte[] { 0x48, 0x00, 0x00, 0x40 }, dol.Skip(end - 4).Take(4).ToArray());
        Assert.IsTrue(before.Take(end - 4).SequenceEqual(dol.Take(end - 4)));
        Assert.IsTrue(before.Skip(end).SequenceEqual(dol.Skip(end)));
        Assert.IsFalse(DolFilterPatches.RemoveDeflicker(dol), "already patched");
    }

    [TestMethod]
    public void RemoveDithering_PatternPresent_RewritesTheInstructionBeforeIt()
    {
        var dol = FakeRetailDisc.Dol();
        var before = (byte[])dol.Clone();

        Assert.IsTrue(DolFilterPatches.RemoveDithering(dol));

        CollectionAssert.AreEqual(new byte[] { 0x48, 0x00, 0x00, 0x28 }, dol.Skip(FakeRetailDisc.DitheringOffset - 4).Take(4).ToArray());
        Assert.IsTrue(before.Take(FakeRetailDisc.DitheringOffset - 4).SequenceEqual(dol.Take(FakeRetailDisc.DitheringOffset - 4)));
        Assert.IsTrue(before.Skip(FakeRetailDisc.DitheringOffset).SequenceEqual(dol.Skip(FakeRetailDisc.DitheringOffset)));
    }

    [TestMethod]
    public void HalveVerticalFilter_ReplacesEveryStockFilter()
    {
        var dol = FakeRetailDisc.Dol();

        Assert.AreEqual(2, DolFilterPatches.HalveVerticalFilter(dol));

        CollectionAssert.AreEqual(new byte[] { 4, 4, 16, 16, 16, 4, 4 }, dol.Skip(FakeRetailDisc.VerticalFilterOffset).Take(7).ToArray());
        CollectionAssert.AreEqual(new byte[] { 4, 4, 16, 16, 16, 4, 4 }, dol.Skip(FakeRetailDisc.RenderModeOffset + 48).Take(7).ToArray());
        Assert.AreEqual(0, DolFilterPatches.HalveVerticalFilter(dol));
    }

    [TestMethod]
    public void FilterPatches_PatternAbsentOrNull_ReportAndThrow()
    {
        var plain = new byte[0x100];

        Assert.IsFalse(DolFilterPatches.RemoveDeflicker(plain));
        Assert.IsFalse(DolFilterPatches.RemoveDithering(plain));
        Assert.AreEqual(0, DolFilterPatches.HalveVerticalFilter(plain));
        Assert.ThrowsExactly<ArgumentNullException>(() => DolFilterPatches.RemoveDeflicker(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => DolFilterPatches.RemoveDithering(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => DolFilterPatches.HalveVerticalFilter(null!));
    }

    [TestMethod]
    public void VideoMode_Pal50_RewritesGeometryAndKeepsSamplesAndFilter()
    {
        var dol = FakeRetailDisc.Dol();

        Assert.AreEqual(1, VideoModePatch.Apply(dol, WiiVideoMode.Pal50));

        CollectionAssert.AreEqual(FakeRetailDisc.Pal528IntDfHeader, dol.Skip(FakeRetailDisc.RenderModeOffset).Take(24).ToArray());
        CollectionAssert.AreEqual(FakeRetailDisc.SamplePattern, dol.Skip(FakeRetailDisc.RenderModeOffset + 24).Take(24).ToArray());
        CollectionAssert.AreEqual(FakeRetailDisc.DefaultVerticalFilter, dol.Skip(FakeRetailDisc.RenderModeOffset + 48).Take(7).ToArray());
        Assert.AreEqual(0, VideoModePatch.Apply(dol, WiiVideoMode.Pal50), "already PAL");
        Assert.AreEqual(1, VideoModePatch.Apply(dol, WiiVideoMode.Ntsc), "and back");
        CollectionAssert.AreEqual(FakeRetailDisc.Ntsc480IntDfHeader, dol.Skip(FakeRetailDisc.RenderModeOffset).Take(24).ToArray());
    }

    [TestMethod]
    public void VideoMode_Pal60_OnlyChangesTheTvModeField()
    {
        var dol = FakeRetailDisc.Dol();

        Assert.AreEqual(1, VideoModePatch.Apply(dol, WiiVideoMode.Pal60));

        var expected = (byte[])FakeRetailDisc.Ntsc480IntDfHeader.Clone();
        expected[3] = 0x14;
        CollectionAssert.AreEqual(expected, dol.Skip(FakeRetailDisc.RenderModeOffset).Take(24).ToArray());
    }

    [TestMethod]
    public void VideoMode_ScaledPal576ToNtsc_MapsToTheInterlacedClass()
    {
        var dol = new byte[0x100];
        var pal576 = new byte[] { 0, 0, 0, 4, 0x02, 0x80, 0x01, 0xE0, 0x02, 0x40, 0x00, 0x28, 0x00, 0x00, 0x02, 0x80, 0x02, 0x40, 0, 0, 0, 1, 0, 0 };
        pal576.CopyTo(dol, 0x40);
        var prog = new byte[] { 0, 0, 0, 0x16, 0x02, 0x80, 0x01, 0xE0, 0x01, 0xE0, 0x00, 0x28, 0x00, 0x00, 0x02, 0x80, 0x01, 0xE0, 0, 0, 0, 0, 0, 0 };
        prog.CopyTo(dol, 0x80);

        Assert.AreEqual(2, VideoModePatch.Apply(dol, WiiVideoMode.Ntsc));

        CollectionAssert.AreEqual(FakeRetailDisc.Ntsc480IntDfHeader, dol.Skip(0x40).Take(24).ToArray());
        var expectedProg = (byte[])prog.Clone();
        expectedProg[3] = 0x02;
        CollectionAssert.AreEqual(expectedProg, dol.Skip(0x80).Take(24).ToArray());
    }

    [TestMethod]
    public void VideoMode_UnchangedUnknownOrNull_LeaveTheDolAlone()
    {
        var dol = FakeRetailDisc.Dol();
        var before = (byte[])dol.Clone();
        var odd = new byte[0x40];
        new byte[] { 0, 0, 0, 0, 0x02, 0x80, 0x01, 0xE1, 0x01, 0xE0, 0x00, 0x28 }.CopyTo(odd, 0);

        Assert.AreEqual(0, VideoModePatch.Apply(dol, WiiVideoMode.Unchanged));
        CollectionAssert.AreEqual(before, dol);
        Assert.AreEqual(0, VideoModePatch.Apply(odd, WiiVideoMode.Pal50), "efbHeight 481 is nothing we know");
        Assert.ThrowsExactly<ArgumentNullException>(() => VideoModePatch.Apply(null!, WiiVideoMode.Pal50));
    }

    [TestMethod]
    public void PatchMainDol_AllOptions_AppliesEachAndReports()
    {
        var options = new WiiOptions { RemoveDeflicker = true, RemoveDithering = true, HalfVerticalFilter = true, VideoMode = WiiVideoMode.Pal60 };
        var messages = new List<string>();
        var original = FakeRetailDisc.Dol();

        var patched = WiiRomInjector.PatchMainDol(original, options, new Progress(messages.Add));

        Assert.AreNotSame(original, patched);
        CollectionAssert.AreEqual(FakeRetailDisc.Dol(), original, "input untouched");
        CollectionAssert.AreEqual(new[] { "Deflicker filter removed", "Dithering removed", "Vertical filters halved: 2", "Video modes set to Pal60: 1" }, messages);
        Assert.IsTrue(WiiRomInjector.PatchesMainDol(options));
        Assert.IsFalse(WiiRomInjector.PatchesMainDol(new WiiOptions()));
        Assert.ThrowsExactly<ArgumentNullException>(() => WiiRomInjector.PatchMainDol(null!, options));
        Assert.ThrowsExactly<ArgumentNullException>(() => WiiRomInjector.PatchMainDol(original, null!));
    }

    private sealed class Progress : IProgress<string>
    {
        private readonly Action<string> _report;

        public Progress(Action<string> report)
        {
            _report = report;
        }

        public void Report(string value) => _report(value);
    }
}
