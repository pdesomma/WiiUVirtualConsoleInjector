using CommunityToolkit.Mvvm.ComponentModel;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Options;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels.Options;

/// <summary>
/// Editable <see cref="GameCubeOptions"/>.
/// </summary>
public sealed partial class GameCubeOptionsViewModel : ConsoleOptionsViewModel
{
    [ObservableProperty]
    private bool _forceFourByThree;

    /// <summary>
    /// Creates a new instance of the <see cref="GameCubeOptionsViewModel"/> class.
    /// </summary>
    /// <param name="dialogs">Picker for the forwarder and second disc.</param>
    public GameCubeOptionsViewModel(IDialogService dialogs)
        : base(SourceConsole.GameCube)
    {
        Forwarder = new PathFieldViewModel(dialogs, "Nintendont forwarder", "empty uses the embedded build", new FileFilter("DOL files", "*.dol"));
        SecondDisc = new PathFieldViewModel(dialogs, "Second disc", "two-disc games", new FileFilter("GameCube images", "*.iso", "*.gcm", "*.gcz"));
    }

    /// <summary>
    /// Forwarder to use as main.dol.
    /// </summary>
    public PathFieldViewModel Forwarder { get; }

    /// <summary>
    /// Second disc image for two-disc games.
    /// </summary>
    public PathFieldViewModel SecondDisc { get; }

    /// <inheritdoc/>
    public override IConsoleOptions? Build() => new GameCubeOptions
    {
        ForceFourByThree = ForceFourByThree,
        ForwarderPath = Forwarder.Path,
        SecondDiscPath = SecondDisc.Path,
    };

    /// <inheritdoc/>
    public override void Load(IConsoleOptions? options)
    {
        var cube = options as GameCubeOptions;
        ForceFourByThree = cube?.ForceFourByThree ?? false;
        Forwarder.Path = cube?.ForwarderPath;
        SecondDisc.Path = cube?.SecondDiscPath;
    }
}
