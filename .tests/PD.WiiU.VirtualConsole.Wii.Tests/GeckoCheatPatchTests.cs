using WiiSharp;

namespace PD.WiiU.VirtualConsole.Wii.Tests;

[TestClass]
public class GeckoCheatPatchTests
{
    private static readonly GeckoCodeLine[] Lines = { new(0x04123456, 0x00000063), new(0xC2222222, 0x00000001), new(0x38600001, 0x00000000) };

    [TestMethod]
    public void Apply_DolWithViHandler_AddsHandlerSectionAndBranchesFromItsBlr()
    {
        var patched = GeckoCheatPatch.Apply(FakeRetailDisc.Dol(), Lines);

        var dol = DolFile.Parse(patched);
        var header = DolHeader.Parse(patched);
        Assert.AreEqual(2, header.TextSections.Count);
        var handler = GeckoCheatPatch.Handler();
        Assert.AreEqual(0xAB0, handler.Length);
        CollectionAssert.AreEqual(handler.Take(0xAA8).ToArray(), dol.Read(GeckoCheatPatch.HandlerAddress, 0xAA8));
        CollectionAssert.AreEqual(new byte[] { 0x80, 0x00 }, dol.Read(GeckoCheatPatch.HandlerAddress + 0x106, 2), "the handler already names the code list address");
        CollectionAssert.AreEqual(new byte[] { 0x22, 0xA8 }, dol.Read(GeckoCheatPatch.HandlerAddress + 0x10A, 2));
        CollectionAssert.AreEqual(GeckoCodes.Build(Lines), dol.Read(GeckoCheatPatch.CodeListAddress, 8 + Lines.Length * 8 + 8));

        var hook = 0x80004000u + FakeRetailDisc.ViHookOffset + 0x10 - 0x100;
        var branch = dol.Read(hook, 4);
        var word = (uint)(branch[0] << 24 | branch[1] << 16 | branch[2] << 8 | branch[3]);
        Assert.AreEqual(0x48000000u, word & 0xFC000003, "an unconditional branch without link");
        var displacement = (int)((word & 0x03FFFFFC) << 6) >> 6;
        Assert.AreEqual(GeckoCheatPatch.HandlerEntry, (uint)(hook + displacement), "lands on the handler entry, which sits below the hook");
        Assert.AreEqual(0x80004000u, header.EntryPoint, "the game still starts where it did");
    }

    [TestMethod]
    public void Apply_NoViHandler_ThrowsInvalidDataException()
    {
        var dol = FakeRetailDisc.Dol();
        Array.Clear(dol, FakeRetailDisc.ViHookOffset, 16);

        var error = Assert.ThrowsExactly<InvalidDataException>(() => GeckoCheatPatch.Apply(dol, Lines));

        StringAssert.Contains(error.Message, "VI");
    }

    [TestMethod]
    public void Apply_AreaAlreadyUsed_ThrowsInvalidDataException()
    {
        var dol = DolFile.Parse(FakeRetailDisc.Dol());
        dol.AddSection(0x80002000, new byte[0x10], text: false);

        Assert.ThrowsExactly<InvalidDataException>(() => GeckoCheatPatch.Apply(dol.ToBytes(), Lines));
    }

    [TestMethod]
    public void Apply_TooManyLinesOrBadArguments_Throw()
    {
        var dol = FakeRetailDisc.Dol();
        var tooMany = Enumerable.Range(0, GeckoCheatPatch.MaxLines + 1).Select(i => new GeckoCodeLine((uint)i, 0)).ToArray();

        Assert.ThrowsExactly<NotSupportedException>(() => GeckoCheatPatch.Apply(dol, tooMany));
        Assert.AreEqual(425, GeckoCheatPatch.MaxLines);
        GeckoCheatPatch.Apply(dol, tooMany.Take(GeckoCheatPatch.MaxLines).ToArray());
        Assert.ThrowsExactly<ArgumentNullException>(() => GeckoCheatPatch.Apply(null!, Lines));
        Assert.ThrowsExactly<ArgumentNullException>(() => GeckoCheatPatch.Apply(dol, null!));
        Assert.ThrowsExactly<ArgumentException>(() => GeckoCheatPatch.Apply(dol, Array.Empty<GeckoCodeLine>()));
    }
}
