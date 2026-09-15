using System.Globalization;
using Avalonia;
using Avalonia.Media;
using WiiUSharp;
using WiiUVirtualConsoleInjector.Converters;

namespace WiiUVirtualConsoleInjector.Tests;

[TestClass]
public class ConverterTests
{
    [TestMethod]
    public void FileImageConverter_NullOrBlankPath_ReturnsNull()
    {
        Assert.IsNull(FileImageConverter.Load(null));
        Assert.IsNull(FileImageConverter.Load(""));
        Assert.IsNull(FileImageConverter.Load("   "));
        Assert.IsNull(FileImageConverter.Bitmap.Convert(null, typeof(object), null, CultureInfo.InvariantCulture));
        Assert.AreEqual(true, FileImageConverter.IsMissing.Convert(null, typeof(bool), null, CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void FileImageConverter_MissingFile_ReturnsNull()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");

        Assert.IsNull(FileImageConverter.Load(path));
        Assert.AreEqual(true, FileImageConverter.IsMissing.Convert(path, typeof(bool), null, CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void FileImageConverter_UndecodableFile_ReturnsNull()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
        File.WriteAllText(path, "not an image");
        try
        {
            Assert.IsNull(FileImageConverter.Load(path));
            Assert.IsNull(FileImageConverter.Load(path));
            Assert.AreEqual(true, FileImageConverter.IsMissing.Convert(path, typeof(bool), null, CultureInfo.InvariantCulture));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void RegionConverters_Europe_IsSolidBlue()
    {
        var brush = (SolidColorBrush)RegionConverters.FlagFor(Region.Europe);

        Assert.AreEqual(Color.Parse("#2B4B9B"), brush.Color);
    }

    [TestMethod]
    public void RegionConverters_Japan_IsRadialRedOnWhite()
    {
        var brush = (RadialGradientBrush)RegionConverters.FlagFor(Region.Japan);

        Assert.AreEqual(Color.Parse("#BC002D"), brush.GradientStops[0].Color);
        Assert.AreEqual(Colors.White, brush.GradientStops[^1].Color);
    }

    [TestMethod]
    public void RegionConverters_UnitedStates_IsHardStripedRedAndWhite()
    {
        var brush = (LinearGradientBrush)RegionConverters.FlagFor(Region.UnitedStates);

        Assert.AreEqual(14, brush.GradientStops.Count);
        Assert.AreEqual(brush.GradientStops[0].Color, brush.GradientStops[1].Color);
        Assert.AreNotEqual(brush.GradientStops[1].Color, brush.GradientStops[2].Color);
        Assert.AreEqual(brush.GradientStops[1].Offset, brush.GradientStops[2].Offset);
        Assert.AreEqual(1.0, brush.GradientStops[^1].Offset);
    }

    [TestMethod]
    public void RegionConverters_OtherRegion_IsGray()
    {
        var brush = (SolidColorBrush)RegionConverters.FlagFor(Region.All);

        Assert.AreEqual(Color.Parse("#9A9A98"), brush.Color);
        Assert.AreNotSame(RegionConverters.FlagFor(Region.Europe), brush);
    }

    [TestMethod]
    public void RegionConverters_Flag_ConvertsEnumAndRejectsOtherInput()
    {
        var converted = RegionConverters.Flag.Convert(Region.Japan, typeof(IBrush), null, CultureInfo.InvariantCulture);

        Assert.AreSame(RegionConverters.FlagFor(Region.Japan), converted);
        Assert.AreEqual(AvaloniaProperty.UnsetValue, RegionConverters.Flag.Convert("nope", typeof(IBrush), null, CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void StatusConverters_Glyph_TintsByState()
    {
        var on = (SolidColorBrush?)StatusConverters.Glyph.Convert(true, typeof(IBrush), null, CultureInfo.InvariantCulture);
        var off = (SolidColorBrush?)StatusConverters.Glyph.Convert(false, typeof(IBrush), null, CultureInfo.InvariantCulture);

        Assert.AreEqual(Color.Parse("#00AACC"), on!.Color);
        Assert.AreEqual(Color.Parse("#BEBEBC"), off!.Color);
    }

    [TestMethod]
    public void StatusConverters_UsableOpacity_HalvesWhenUnusable()
    {
        Assert.AreEqual(1.0, StatusConverters.UsableOpacity.Convert(true, typeof(double), null, CultureInfo.InvariantCulture));
        Assert.AreEqual(0.5, StatusConverters.UsableOpacity.Convert(false, typeof(double), null, CultureInfo.InvariantCulture));
    }
}
