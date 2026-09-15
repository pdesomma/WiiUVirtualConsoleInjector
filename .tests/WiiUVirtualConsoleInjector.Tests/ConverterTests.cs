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
    public void RegionConverters_UnitedStates_IsStripesWithACanton()
    {
        var brush = (DrawingBrush)RegionConverters.FlagFor(Region.UnitedStates);
        var drawings = ((DrawingGroup)brush.Drawing!).Children.OfType<GeometryDrawing>().ToArray();

        Assert.AreEqual(6, drawings.Length, "white field, four red stripes, blue canton");
        Assert.AreEqual(Brushes.White, drawings[0].Brush);
        Assert.AreEqual(Color.Parse("#3C3B6E"), ((SolidColorBrush)drawings[^1].Brush!).Color);
        var canton = ((RectangleGeometry)drawings[^1].Geometry!).Rect;
        var field = ((RectangleGeometry)drawings[0].Geometry!).Rect;
        Assert.IsTrue(canton.Width < field.Width / 2 && canton.Height < field.Height * 0.6);
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
