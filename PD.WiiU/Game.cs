namespace PD.WiiU;

/// <summary>
/// Identity and presentation metadata the system menu and loader read from an installed title.
/// </summary>
public sealed class Game
{
    /// <summary>
    /// Company code Nintendo uses for its own titles.
    /// </summary>
    public const string NintendoCompanyCode = "0001";

    /// <summary>
    /// Creates a new instance of the <see cref="Game"/> class.
    /// </summary>
    /// <param name="titleId">Uniquely identifies the title on the console and in the eShop.</param>
    /// <param name="groupId">Ties the title to its updates and DLC.</param>
    /// <param name="productCode">The product code shown in system settings and on packaging.</param>
    public Game(TitleId titleId, GroupId groupId, ProductCode productCode)
    {
        TitleId = titleId;
        GroupId = groupId;
        ProductCode = productCode;
    }

    /// <summary>
    /// Four-character publisher code; <see cref="NintendoCompanyCode"/> for Nintendo.
    /// </summary>
    public string CompanyCode { get; init; } = NintendoCompanyCode;
    /// <summary>
    /// How the title uses the GamePad, as the raw value the system reads. Values seen in Nintendo titles are 0, 1 and 65537 (0x10001); the system does not document their meaning.
    /// </summary>
    public uint GamePadUse { get; init; }
    /// <summary>
    /// Ties the title to its updates and DLC.
    /// </summary>
    public GroupId GroupId { get; }
    /// <summary>
    /// The title's name in each language it provides one for.
    /// </summary>
    public IReadOnlyDictionary<Language, LocalizedName> Names { get; init; } = new Dictionary<Language, LocalizedName>();
    /// <summary>
    /// The product code shown in system settings and on packaging.
    /// </summary>
    public ProductCode ProductCode { get; }
    /// <summary>
    /// Which consoles may run the title.
    /// </summary>
    public Region Region { get; init; } = Region.All;
    /// <summary>
    /// Uniquely identifies the title on the console and in the eShop.
    /// </summary>
    public TitleId TitleId { get; }
    /// <summary>
    /// Version of the title data; updates carry a higher number than the base title.
    /// </summary>
    public ushort TitleVersion { get; init; }

    /// <summary>
    /// The name in <paramref name="language"/>, or <see langword="null"/> if the title has none for it.
    /// </summary>
    public LocalizedName? NameIn(Language language) => Names.TryGetValue(language, out var name) ? name : null;
}
