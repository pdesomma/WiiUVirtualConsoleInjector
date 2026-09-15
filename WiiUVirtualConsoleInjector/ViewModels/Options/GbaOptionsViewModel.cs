using CommunityToolkit.Mvvm.ComponentModel;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Options;

namespace WiiUVirtualConsoleInjector.ViewModels.Options;

/// <summary>
/// Editable <see cref="GbaOptions"/>.
/// </summary>
public sealed partial class GbaOptionsViewModel : ConsoleOptionsViewModel
{
    [ObservableProperty]
    private bool _pokemonPatch;
    [ObservableProperty]
    private bool _removeDarkFilter;

    /// <summary>
    /// Creates a new instance of the <see cref="GbaOptionsViewModel"/> class.
    /// </summary>
    public GbaOptionsViewModel()
        : base(SourceConsole.Gba)
    {
    }

    /// <inheritdoc/>
    public override IConsoleOptions? Build() => new GbaOptions { PokemonPatch = PokemonPatch, RemoveDarkFilter = RemoveDarkFilter };
}
