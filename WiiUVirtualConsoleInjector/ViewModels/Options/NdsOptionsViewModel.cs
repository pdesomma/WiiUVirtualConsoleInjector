using CommunityToolkit.Mvvm.ComponentModel;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Options;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels.Options;

/// <summary>
/// Editable <see cref="NdsOptions"/>.
/// </summary>
public sealed partial class NdsOptionsViewModel : ConsoleOptionsViewModel
{
    [ObservableProperty]
    private int _brightness = NdsOptions.DefaultBrightness;
    [ObservableProperty]
    private NdsLayoutPack _layoutPack;
    [ObservableProperty]
    private int _pixelArtUpscaler;

    /// <summary>
    /// Creates a new instance of the <see cref="NdsOptionsViewModel"/> class.
    /// </summary>
    /// <param name="dialogs">Picker for the layout folder.</param>
    public NdsOptionsViewModel(IDialogService dialogs)
        : base(SourceConsole.Nds)
    {
        LayoutScreens = new PathFieldViewModel(dialogs, "Layout screens", "folder copied over the title");
    }

    /// <summary>
    /// Folder of replacement layout screens; empty keeps the base's own.
    /// </summary>
    /// <summary>
    /// Packs to choose from.
    /// </summary>
    public IReadOnlyList<NdsLayoutPack> LayoutPacks { get; } = new[] { NdsLayoutPack.None, NdsLayoutPack.All, NdsLayoutPack.PhantomHourglass };
    /// <summary>
    /// Custom layout folder; overrides the pack.
    /// </summary>
    public PathFieldViewModel LayoutScreens { get; }

    /// <inheritdoc/>
    public override IConsoleOptions? Build() => new NdsOptions
    {
        Brightness = Brightness,
        LayoutPack = LayoutPack,
        LayoutScreensPath = LayoutScreens.Path,
        PixelArtUpscaler = PixelArtUpscaler,
    };

    /// <inheritdoc/>
    public override void Load(IConsoleOptions? options)
    {
        var nds = options as NdsOptions;
        Brightness = nds?.Brightness ?? NdsOptions.DefaultBrightness;
        LayoutPack = nds?.LayoutPack ?? NdsLayoutPack.None;
        LayoutScreens.Path = nds?.LayoutScreensPath;
        PixelArtUpscaler = nds?.PixelArtUpscaler ?? 0;
    }
}
