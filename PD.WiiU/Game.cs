namespace PD.WiiU;

/// <summary>
/// A game as the Wii U sees it: the identity and presentation metadata the system menu and
/// loader read from an installed title. Deliberately says nothing about where the game came
/// from or how it was built — that belongs to whatever produces the <see cref="Game"/>.
/// </summary>
public sealed class Game
{
    /// <summary>Company code Nintendo uses for its own titles.</summary>
    public const string NintendoCompanyCode = "0001";

    /// <summary>Uniquely identifies the title on the console and in the eShop.</summary>
    public TitleId TitleId { get; }

    /// <summary>Ties the title to its updates and DLC.</summary>
    public GroupId GroupId { get; }

    /// <summary>The product code shown in system settings and on packaging.</summary>
    public ProductCode ProductCode { get; }

    /// <summary>Four-character publisher code; <see cref="NintendoCompanyCode"/> for Nintendo.</summary>
    public string CompanyCode { get; init; } = NintendoCompanyCode;

    /// <summary>Version of the title data; updates carry a higher number than the base title.</summary>
    public ushort TitleVersion { get; init; }

    /// <summary>Which consoles may run the title.</summary>
    public Region Region { get; init; } = Region.All;

    /// <summary>
    /// How the title uses the GamePad, as the raw value the system reads. Values seen in
    /// Nintendo titles are 0, 1 and 65537 (0x10001); the system does not document their meaning.
    /// </summary>
    public uint GamePadUse { get; init; }

    /// <summary>The title's name in each language it provides one for.</summary>
    public IReadOnlyDictionary<Language, LocalizedName> Names { get; init; } = new Dictionary<Language, LocalizedName>();

    public Game(TitleId titleId, GroupId groupId, ProductCode productCode)
    {
        TitleId = titleId;
        GroupId = groupId;
        ProductCode = productCode;
    }

    /// <summary>The name in <paramref name="language"/>, or <see langword="null"/> if the title has none for it.</summary>
    public LocalizedName? NameIn(Language language) =>
        Names.TryGetValue(language, out var name) ? name : null;
}
