namespace PD.WiiU.VirtualConsole.Options;

/// <summary>
/// Game Boy Advance settings.
/// </summary>
public sealed class GbaOptions : IConsoleOptions
{
    /// <inheritdoc/>
    public SourceConsole Console => SourceConsole.Gba;
    /// <summary>
    /// Apply the Pokémon save patch.
    /// </summary>
    public bool PokemonPatch { get; init; }
    /// <summary>
    /// Switch off the darkening the base applies to the game.
    /// </summary>
    public bool RemoveDarkFilter { get; init; }
}
