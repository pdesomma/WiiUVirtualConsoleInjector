using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using WiiUSharp;

namespace WiiUVirtualConsoleInjector.Converters;

/// <summary>
/// Turns a <see cref="Region"/> into the brush of its flag swatch.
/// </summary>
public static class RegionConverters
{
    private const int StripeCount = 7;

    private static readonly IBrush EuropeFlag = new SolidColorBrush(Color.Parse("#2B4B9B"));
    private static readonly IBrush JapanFlag = new RadialGradientBrush
    {
        Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
        GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
        RadiusX = new RelativeScalar(0.5, RelativeUnit.Relative),
        RadiusY = new RelativeScalar(0.5, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Color.Parse("#BC002D"), 0),
            new GradientStop(Color.Parse("#BC002D"), 0.42),
            new GradientStop(Colors.White, 0.44),
            new GradientStop(Colors.White, 1),
        },
    };
    private static readonly IBrush OtherFlag = new SolidColorBrush(Color.Parse("#9A9A98"));
    private static readonly IBrush UnitedStatesFlag = StarsAndStripes();

    /// <summary>
    /// Region to its flag brush; gray for regions without one.
    /// </summary>
    public static readonly IValueConverter Flag = new FuncValueConverter<Region, IBrush>(FlagFor);

    /// <summary>
    /// Brush drawn for a region's swatch.
    /// </summary>
    /// <param name="region">Region of the base.</param>
    public static IBrush FlagFor(Region region) => region switch
    {
        Region.UnitedStates => UnitedStatesFlag,
        Region.Europe => EuropeFlag,
        Region.Japan => JapanFlag,
        _ => OtherFlag,
    };

    /// <summary>
    /// Seven stripes with a blue canton over the top four, in flag proportions.
    /// </summary>
    private static IBrush StarsAndStripes()
    {
        const double width = 190;
        const double height = 100;
        var red = new SolidColorBrush(Color.Parse("#B22234"));
        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing { Brush = Brushes.White, Geometry = new RectangleGeometry(new Rect(0, 0, width, height)) });
        var stripe = height / StripeCount;
        for (var i = 0; i < StripeCount; i += 2)
            group.Children.Add(new GeometryDrawing { Brush = red, Geometry = new RectangleGeometry(new Rect(0, i * stripe, width, stripe)) });
        group.Children.Add(new GeometryDrawing { Brush = new SolidColorBrush(Color.Parse("#3C3B6E")), Geometry = new RectangleGeometry(new Rect(0, 0, width * 0.4, stripe * 4)) });
        return new DrawingBrush(group) { Stretch = Stretch.Fill };
    }
}
