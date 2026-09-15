using Avalonia.Data.Converters;
using Avalonia.Media;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.Converters;

/// <summary>
/// Converters for the toast stack.
/// </summary>
public static class ToastConverters
{
    private static readonly IBrush Cyan = new SolidColorBrush(Color.Parse("#00AACC"));
    private static readonly IBrush Red = new SolidColorBrush(Color.Parse("#C0392B"));
    private static readonly IBrush Amber = new SolidColorBrush(Color.Parse("#F5A623"));

    /// <summary>
    /// The kind's colour for the toast's left stripe.
    /// </summary>
    public static readonly IValueConverter Stripe = new FuncValueConverter<ToastKind, IBrush>(StripeFor);

    /// <summary>
    /// The stripe colour of one kind.
    /// </summary>
    /// <param name="kind">Kind shown.</param>
    public static IBrush StripeFor(ToastKind kind) => kind switch
    {
        ToastKind.Error => Red,
        ToastKind.Warning => Amber,
        _ => Cyan,
    };
}
