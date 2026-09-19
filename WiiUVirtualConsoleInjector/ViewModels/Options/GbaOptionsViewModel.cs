using CommunityToolkit.Mvvm.ComponentModel;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Options;

namespace WiiUVirtualConsoleInjector.ViewModels.Options;

/// <summary>
/// Editable <see cref="GbaOptions"/>, for the Game Boy Advance or the Game Boy on its base.
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
    /// <param name="console">Game Boy Advance, or Game Boy for a Goomba inject.</param>
    public GbaOptionsViewModel(SourceConsole console = SourceConsole.Gba)
        : base(console)
    {
    }

    /// <inheritdoc/>
    public override IConsoleOptions? Build() => new GbaOptions { Console = Console, PokemonPatch = PokemonPatch, RemoveDarkFilter = RemoveDarkFilter };

    /// <inheritdoc/>
    public override void Load(IConsoleOptions? options)
    {
        var gba = options as GbaOptions;
        PokemonPatch = gba?.PokemonPatch ?? false;
        RemoveDarkFilter = gba?.RemoveDarkFilter ?? false;
    }
}
