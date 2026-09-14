using PD.WiiU.VirtualConsole.Options;
using WiiSharp;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Wii.Tests;

[TestClass]
public class RegionPatcherTests
{
    [TestMethod]
    public void ApplyWritesThePresetForTheTargetRegion()
    {
        var disc = new MemoryStream(new byte[0x50000]);

        var changed = RegionPatcher.Apply(disc, new WiiOptions { TargetRegion = Region.UnitedStates });

        Assert.IsTrue(changed);
        Assert.AreEqual(0x01, disc.ToArray()[0x4E003]);
        Assert.AreEqual(0x06, disc.ToArray()[0x4E011]);
        Assert.AreEqual(DiscRegion.UnitedStates, RegionArea.Read(disc).Region);
    }

    [TestMethod]
    public void ApplyLeavesTheImageAloneWithoutATarget()
    {
        var disc = new MemoryStream(new byte[0x50000]);

        var changed = RegionPatcher.Apply(disc, new WiiOptions());

        Assert.IsFalse(changed);
        Assert.IsTrue(disc.ToArray().All(b => b == 0));
    }

    [TestMethod]
    public void EverythingButJapanAndTheStatesBecomesEurope()
    {
        Assert.AreEqual(DiscRegion.Japan, RegionPatcher.ToDiscRegion(Region.Japan));
        Assert.AreEqual(DiscRegion.UnitedStates, RegionPatcher.ToDiscRegion(Region.UnitedStates));
        Assert.AreEqual(DiscRegion.Europe, RegionPatcher.ToDiscRegion(Region.Europe));
        Assert.AreEqual(DiscRegion.Europe, RegionPatcher.ToDiscRegion(Region.Australia));
        Assert.AreEqual(DiscRegion.Europe, RegionPatcher.ToDiscRegion(Region.All));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => RegionPatcher.ToDiscRegion(Region.None));
    }
}
