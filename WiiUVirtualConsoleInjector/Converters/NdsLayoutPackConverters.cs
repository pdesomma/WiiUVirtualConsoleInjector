using Avalonia.Data.Converters;
using PD.WiiU.VirtualConsole.Options;

namespace WiiUVirtualConsoleInjector.Converters;

/// <summary>
/// Labels for the DS layout packs.
/// </summary>
public static class NdsLayoutPackConverters
{
    /// <summary>
    /// The pack as the menu shows it.
    /// </summary>
    public static readonly IValueConverter Label = new FuncValueConverter<NdsLayoutPack, string>(pack => pack switch
    {
        NdsLayoutPack.All => "All games",
        NdsLayoutPack.PhantomHourglass => "Phantom Hourglass",
        _ => "None (the base's own)",
    });
}
