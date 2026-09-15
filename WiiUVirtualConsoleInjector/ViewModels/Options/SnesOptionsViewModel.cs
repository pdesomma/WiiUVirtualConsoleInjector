using CommunityToolkit.Mvvm.ComponentModel;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Options;

namespace WiiUVirtualConsoleInjector.ViewModels.Options;

/// <summary>
/// Editable <see cref="SnesOptions"/>.
/// </summary>
public sealed partial class SnesOptionsViewModel : ConsoleOptionsViewModel
{
    [ObservableProperty]
    private bool _pixelPerfect;

    /// <summary>
    /// Creates a new instance of the <see cref="SnesOptionsViewModel"/> class.
    /// </summary>
    public SnesOptionsViewModel()
        : base(SourceConsole.Snes)
    {
    }

    /// <inheritdoc/>
    public override IConsoleOptions? Build() => new SnesOptions { PixelPerfect = PixelPerfect };

    /// <inheritdoc/>
    public override void Load(IConsoleOptions? options) => PixelPerfect = (options as SnesOptions)?.PixelPerfect ?? false;
}
