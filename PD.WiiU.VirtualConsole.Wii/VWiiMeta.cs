using System.Text;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole.Wii;

/// <summary>
/// The meta.xml fields a vWii title carries about the disc it wraps.
/// </summary>
public static class VWiiMeta
{
    /// <summary>
    /// drc_use value letting the GamePad act as a controller in Wii mode.
    /// </summary>
    public const string GamePadAsController = "65537";
    /// <summary>
    /// Element holding the hex-encoded four-character game code.
    /// </summary>
    public const string GameCodeElement = "reserved_flag2";
    /// <summary>
    /// Element controlling GamePad use.
    /// </summary>
    public const string GamePadElement = "drc_use";

    /// <summary>
    /// Hex encoding of the first four characters of a game ID, the form the manual lookup expects.
    /// </summary>
    /// <param name="gameId">Six-character game ID.</param>
    public static string EncodeGameCode(string gameId)
    {
        if (gameId is null)
            throw new ArgumentNullException(nameof(gameId));
        if (gameId.Length < 4)
            throw new ArgumentException("Game ID needs at least four characters.", nameof(gameId));

        var hex = new StringBuilder();
        foreach (var b in Encoding.ASCII.GetBytes(gameId.Substring(0, 4)))
            hex.Append(b.ToString("x2"));
        return hex.ToString();
    }

    /// <summary>
    /// Stores the game code, and optionally enables the GamePad as a controller.
    /// </summary>
    /// <param name="title">Staged title.</param>
    /// <param name="gameId">Six-character game ID.</param>
    /// <param name="gamePadAsController">Set drc_use to <see cref="GamePadAsController"/>.</param>
    public static void Apply(TitleDirectory title, string gameId, bool gamePadAsController = false)
    {
        if (title is null)
            throw new ArgumentNullException(nameof(title));

        var meta = MetaXml.Load(title.MetaXmlPath);
        meta.Set(GameCodeElement, EncodeGameCode(gameId));
        if (gamePadAsController)
            meta.Set(GamePadElement, GamePadAsController);
        meta.Save(title.MetaXmlPath);
    }
}
