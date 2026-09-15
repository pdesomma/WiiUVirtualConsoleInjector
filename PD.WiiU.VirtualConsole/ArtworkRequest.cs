namespace PD.WiiU.VirtualConsole;

/// <summary>
/// What to draw on a generated icon or boot screen.
/// </summary>
public sealed class ArtworkRequest
{
    /// <summary>
    /// Creates a new instance of the <see cref="ArtworkRequest"/> class.
    /// </summary>
    /// <param name="template">Frame to dress the screenshot in.</param>
    public ArtworkRequest(ArtworkTemplate template)
    {
        Template = template ?? throw new ArgumentNullException(nameof(template));
    }

    /// <summary>
    /// First line of the game's name, or null.
    /// </summary>
    public string? NameLine1 { get; init; }
    /// <summary>
    /// Second line of the game's name, or null.
    /// </summary>
    public string? NameLine2 { get; init; }
    /// <summary>
    /// Highest player count to advertise, or null to leave it off.
    /// </summary>
    public int? Players { get; init; }
    /// <summary>
    /// Year to print, or null to leave it off.
    /// </summary>
    public int? ReleaseYear { get; init; }
    /// <summary>
    /// Screenshot to sit inside the frame; null leaves the screen black.
    /// </summary>
    public string? ScreenshotPath { get; init; }
    /// <summary>
    /// Frame to dress the screenshot in.
    /// </summary>
    public ArtworkTemplate Template { get; }

    /// <summary>
    /// True when a name line carries Japanese, which is what the console's own screens caption in Japanese.
    /// </summary>
    public bool IsJapanese => HasJapanese(NameLine1) || HasJapanese(NameLine2);

    /// <summary>
    /// The players line as the boot screen prints it, or null.
    /// </summary>
    public string? PlayersText => Players is not > 0
        ? null
        : IsJapanese
            ? "プレイ人数　" + PlayerRange + "人"
            : "Players: " + PlayerRange;
    /// <summary>
    /// The release line as the boot screen prints it, or null.
    /// </summary>
    public string? ReleasedText => ReleaseYear is not > 0
        ? null
        : IsJapanese
            ? ReleaseYear + "年発売"
            : "Released: " + ReleaseYear;

    private string PlayerRange => Players >= 4 ? "1-4" : Players == 1 ? "1" : "1-" + Players;

    /// <summary>
    /// True when the text holds kana or kanji.
    /// </summary>
    /// <param name="text">Text to test.</param>
    private static bool HasJapanese(string? text) =>
        text is not null && text.Any(c => c is >= '぀' and <= 'ヿ' or >= '一' and <= '鿿');
}
