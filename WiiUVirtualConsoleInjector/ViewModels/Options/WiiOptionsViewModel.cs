using CommunityToolkit.Mvvm.ComponentModel;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Options;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels.Options;

/// <summary>
/// Editable <see cref="WiiOptions"/>.
/// </summary>
public sealed partial class WiiOptionsViewModel : ConsoleOptionsViewModel
{
    [ObservableProperty]
    private WiiControllerMode _controllerMode = WiiControllerMode.ClassicController;
    [ObservableProperty]
    private bool _forceFourByThree;
    [ObservableProperty]
    private bool _halfVerticalFilter;
    [ObservableProperty]
    private bool _lrPatch;
    [ObservableProperty]
    private bool _passthrough = true;
    [ObservableProperty]
    private bool _removeDeflicker;
    [ObservableProperty]
    private bool _removeDithering;
    [ObservableProperty]
    private RegionChoice _targetRegion = RegionChoice.All[0];
    [ObservableProperty]
    private bool _trimDisc = true;
    [ObservableProperty]
    private WiiVideoMode _videoMode;

    /// <summary>
    /// Creates a new instance of the <see cref="WiiOptionsViewModel"/> class.
    /// </summary>
    /// <param name="dialogs">Picker for the cheat file and booter.</param>
    public WiiOptionsViewModel(IDialogService dialogs)
        : base(SourceConsole.Wii)
    {
        CheatCodes = new PathFieldViewModel(dialogs, "Cheat codes", "Gecko codes: .gct, Ocarina .txt or a Dolphin .ini", new FileFilter("Cheat files", "*.gct", "*.txt", "*.ini"));
        Forwarder = new PathFieldViewModel(dialogs, "Channel booter", "WAD only; empty uses the embedded one", new FileFilter("DOL files", "*.dol"));
    }

    /// <summary>
    /// Cheat file to apply.
    /// </summary>
    public PathFieldViewModel CheatCodes { get; }

    /// <summary>
    /// Controller modes offered.
    /// </summary>
    public IReadOnlyList<WiiControllerMode> ControllerModes { get; } = Enum.GetValues<WiiControllerMode>();

    /// <summary>
    /// Booter to use as main.dol for a channel WAD.
    /// </summary>
    public PathFieldViewModel Forwarder { get; }

    /// <summary>
    /// Target regions offered.
    /// </summary>
    public IReadOnlyList<RegionChoice> TargetRegions => RegionChoice.All;

    /// <summary>
    /// Video modes offered.
    /// </summary>
    public IReadOnlyList<WiiVideoMode> VideoModes { get; } = Enum.GetValues<WiiVideoMode>();

    /// <inheritdoc/>
    public override IConsoleOptions? Build() => new WiiOptions
    {
        CheatCodesPath = CheatCodes.Path,
        ControllerMode = ControllerMode,
        ForceFourByThree = ForceFourByThree,
        ForwarderPath = Forwarder.Path,
        HalfVerticalFilter = HalfVerticalFilter,
        LrPatch = LrPatch,
        Passthrough = Passthrough,
        RemoveDeflicker = RemoveDeflicker,
        RemoveDithering = RemoveDithering,
        TargetRegion = TargetRegion.Region,
        TrimDisc = TrimDisc,
        VideoMode = VideoMode,
    };

    /// <inheritdoc/>
    public override void Load(IConsoleOptions? options)
    {
        var wii = options as WiiOptions ?? new WiiOptions();
        CheatCodes.Path = wii.CheatCodesPath;
        ControllerMode = wii.ControllerMode;
        ForceFourByThree = wii.ForceFourByThree;
        Forwarder.Path = wii.ForwarderPath;
        HalfVerticalFilter = wii.HalfVerticalFilter;
        LrPatch = wii.LrPatch;
        Passthrough = wii.Passthrough;
        RemoveDeflicker = wii.RemoveDeflicker;
        RemoveDithering = wii.RemoveDithering;
        TargetRegion = RegionChoice.All.FirstOrDefault(r => r.Region == wii.TargetRegion) ?? RegionChoice.All[0];
        TrimDisc = wii.TrimDisc;
        VideoMode = wii.VideoMode;
    }
}
