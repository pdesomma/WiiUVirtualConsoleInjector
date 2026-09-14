using WiiUSharp.Rpx;

namespace PD.WiiU.VirtualConsole.Retro.Tests;

[TestClass]
public class RomSlotTests
{
    [TestMethod]
    public void Find_NesExecutable_LocatesSlotAfterMagicWithHeaderAllowance()
    {
        var rpx = RpxFile.Parse(FakeVcRpx.Build(nes: true));

        var slot = RomSlot.Find(rpx);

        Assert.IsTrue(slot.IsNes);
        Assert.AreSame(rpx.FindSection(".rodata"), slot.Section);
        Assert.AreEqual(FakeVcRpx.RomOffset(nes: true), slot.Offset);
        Assert.AreEqual(FakeVcRpx.SlotCapacity + 16, slot.Capacity);
    }

    [TestMethod]
    public void Find_SnesExecutable_LocatesSlotTwelveBytesAfterMarker()
    {
        var rpx = RpxFile.Parse(FakeVcRpx.Build(nes: false));

        var slot = RomSlot.Find(rpx);

        Assert.IsFalse(slot.IsNes);
        Assert.AreEqual(FakeVcRpx.RomOffset(nes: false), slot.Offset);
        Assert.AreEqual(FakeVcRpx.SlotCapacity, slot.Capacity);
    }

    [TestMethod]
    public void Find_NoMarker_ThrowsInvalidDataException()
    {
        var rpx = RpxFile.Parse(FakeVcRpx.Build(nes: false));
        var rodata = rpx.FindSection(".rodata")!;
        var data = rodata.Data;
        data[0x40] = 0;
        rodata.Data = data;

        Assert.ThrowsExactly<InvalidDataException>(() => RomSlot.Find(rpx));
    }

    [TestMethod]
    public void Find_UnknownSizeKey_ThrowsInvalidDataException()
    {
        var rpx = RpxFile.Parse(FakeVcRpx.Build(nes: false));
        var rodata = rpx.FindSection(".rodata")!;
        var data = rodata.Data;
        data[0x40 - 22] = 0x77;
        rodata.Data = data;

        Assert.ThrowsExactly<InvalidDataException>(() => RomSlot.Find(rpx));
    }

    [TestMethod]
    public void Find_NullRpx_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => RomSlot.Find(null!));
    }

    [TestMethod]
    public void Write_SmallerRom_OverwritesStartAndKeepsTheRest()
    {
        var rpx = RpxFile.Parse(FakeVcRpx.Build(nes: false));
        var slot = RomSlot.Find(rpx);
        var before = slot.Section.Data;
        var rom = FakeVcRpx.SnesRom(0x4000).Select(b => (byte)~b).ToArray();

        slot.Write(rom);

        var after = slot.Section.Data;
        CollectionAssert.AreEqual(rom, after.Skip(slot.Offset).Take(rom.Length).ToArray());
        CollectionAssert.AreEqual(before.Take(slot.Offset).ToArray(), after.Take(slot.Offset).ToArray());
        CollectionAssert.AreEqual(before.Skip(slot.Offset + rom.Length).ToArray(), after.Skip(slot.Offset + rom.Length).ToArray());
    }

    [TestMethod]
    public void Write_RomAtCapacity_Fits()
    {
        var slot = RomSlot.Find(RpxFile.Parse(FakeVcRpx.Build(nes: true)));

        slot.Write(FakeVcRpx.NesRom(slot.Capacity));

        Assert.AreEqual(0x1A, slot.Section.Data[slot.Offset + 3]);
    }

    [TestMethod]
    public void Write_RomTooLarge_ThrowsArgumentException()
    {
        var slot = RomSlot.Find(RpxFile.Parse(FakeVcRpx.Build(nes: true)));

        Assert.ThrowsExactly<ArgumentException>(() => slot.Write(FakeVcRpx.NesRom(slot.Capacity + 1)));
    }

    [TestMethod]
    public void Write_ConsoleMismatch_ThrowsArgumentException()
    {
        var nes = RomSlot.Find(RpxFile.Parse(FakeVcRpx.Build(nes: true)));
        var snes = RomSlot.Find(RpxFile.Parse(FakeVcRpx.Build(nes: false)));

        Assert.ThrowsExactly<ArgumentException>(() => nes.Write(FakeVcRpx.SnesRom(0x1000)));
        Assert.ThrowsExactly<ArgumentException>(() => snes.Write(FakeVcRpx.NesRom(0x1000)));
    }

    [TestMethod]
    public void Write_NullRom_ThrowsArgumentNullException()
    {
        var slot = RomSlot.Find(RpxFile.Parse(FakeVcRpx.Build(nes: true)));

        Assert.ThrowsExactly<ArgumentNullException>(() => slot.Write(null!));
    }
}
