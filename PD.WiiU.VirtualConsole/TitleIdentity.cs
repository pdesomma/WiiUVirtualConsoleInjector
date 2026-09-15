using WiiUSharp;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// The three ids that make a title the same title to the Wii U menu.
/// </summary>
/// <param name="TitleId">Title id.</param>
/// <param name="GroupId">Group shared with updates and DLC.</param>
/// <param name="ProductCode">Product code, e.g. WUP-N-ABCD.</param>
public sealed record TitleIdentity(TitleId TitleId, GroupId GroupId, ProductCode ProductCode)
{
    /// <summary>
    /// The identity a game was built with.
    /// </summary>
    /// <param name="game">Game to read.</param>
    public static TitleIdentity Of(Game game)
    {
        if (game is null)
            throw new ArgumentNullException(nameof(game));

        return new TitleIdentity(game.TitleId, game.GroupId, game.ProductCode);
    }
}
