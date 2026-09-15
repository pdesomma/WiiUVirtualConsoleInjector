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
    private static readonly IBrush UnitedStatesFlag = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
        GradientStops = Stripes(),
    };

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
    /// Hard-edged red and white bands, red first.
    /// </summary>
    private static GradientStops Stripes()
    {
        var red = Color.Parse("#B22234");
        var stops = new GradientStops();
        for (var i = 0; i < StripeCount; i++)
        {
            var color = i % 2 == 0 ? red : Colors.White;
            stops.Add(new GradientStop(color, (double)i / StripeCount));
            stops.Add(new GradientStop(color, (double)(i + 1) / StripeCount));
        }
        return stops;
    }
}
