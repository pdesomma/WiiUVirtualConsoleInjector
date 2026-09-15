using CommunityToolkit.Mvvm.ComponentModel;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Options;

namespace WiiUVirtualConsoleInjector.ViewModels.Options;

/// <summary>
/// Editable <see cref="NesOptions"/>.
/// </summary>
public sealed partial class NesOptionsViewModel : ConsoleOptionsViewModel
{
    [ObservableProperty]
    private bool _pixelPerfect;

    /// <summary>
    /// Creates a new instance of the <see cref="NesOptionsViewModel"/> class.
    /// </summary>
    public NesOptionsViewModel()
        : base(SourceConsole.Nes)
    {
    }

    /// <inheritdoc/>
    public override IConsoleOptions? Build() => new NesOptions { PixelPerfect = PixelPerfect };

    /// <inheritdoc/>
    public override void Load(IConsoleOptions? options) => PixelPerfect = (options as NesOptions)?.PixelPerfect ?? false;
}
