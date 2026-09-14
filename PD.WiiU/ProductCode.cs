namespace PD.WiiU;

/// <summary>
/// The human-facing code printed on packaging and shown in system settings, e.g.
/// <c>WUP-P-ARKE</c> for a retail disc or <c>WUP-N-FAAE</c> for an eShop title.
/// Format is <c>WUP-&lt;category&gt;-&lt;four-character id&gt;</c>.
/// </summary>
public readonly struct ProductCode : IEquatable<ProductCode>
{
    /// <summary>Every Wii U product code starts with this platform tag.</summary>
    public const string Platform = "WUP";

    /// <summary>Category letter for retail (packaged) titles.</summary>
    public const char Retail = 'P';

    /// <summary>Category letter for eShop-distributed titles, including Virtual Console.</summary>
    public const char EShop = 'N';

    private const int IdLength = 4;

    /// <summary>Single letter describing how the title is distributed; see <see cref="Retail"/> and <see cref="EShop"/>.</summary>
    public char Category { get; }

    /// <summary>The four upper-case alphanumeric characters that identify the title.</summary>
    public string Id { get; }

    /// <exception cref="ArgumentException">
    /// <paramref name="category"/> is not an upper-case letter, or <paramref name="id"/> is not
    /// exactly four upper-case alphanumeric characters.
    /// </exception>
    public ProductCode(char category, string id)
    {
        if (!IsUpperLetter(category))
            throw new ArgumentException("Category must be an upper-case letter.", nameof(category));
        if (id is null)
            throw new ArgumentNullException(nameof(id));
        if (!IsValidId(id))
            throw new ArgumentException($"Id must be exactly {IdLength} upper-case alphanumeric characters.", nameof(id));

        Category = category;
        Id = id;
    }

    /// <summary>Parses the <c>WUP-X-XXXX</c> textual form.</summary>
    /// <exception cref="FormatException">The text does not match <c>WUP-X-XXXX</c>.</exception>
    public static ProductCode Parse(string text)
    {
        if (!TryParse(text, out var code))
            throw new FormatException($"'{text}' is not a product code; expected {Platform}-X-XXXX.");
        return code;
    }

    /// <summary>Parses the <c>WUP-X-XXXX</c> textual form without throwing.</summary>
    public static bool TryParse(string? text, out ProductCode code)
    {
        code = default;
        if (text is null)
            return false;

        var parts = text.Split('-');
        if (parts.Length != 3 || parts[0] != Platform || parts[1].Length != 1)
            return false;

        var category = parts[1][0];
        if (!IsUpperLetter(category) || !IsValidId(parts[2]))
            return false;

        code = new ProductCode(category, parts[2]);
        return true;
    }

    /// <summary>The <c>WUP-X-XXXX</c> form the Wii U expects.</summary>
    public override string ToString() => $"{Platform}-{Category}-{Id}";

    public bool Equals(ProductCode other) => Category == other.Category && Id == other.Id;
    public override bool Equals(object? obj) => obj is ProductCode other && Equals(other);
    public override int GetHashCode() => (Category, Id).GetHashCode();
    public static bool operator ==(ProductCode left, ProductCode right) => left.Equals(right);
    public static bool operator !=(ProductCode left, ProductCode right) => !left.Equals(right);

    private static bool IsValidId(string id) => id.Length == IdLength && id.All(IsUpperAlphanumeric);
    private static bool IsUpperLetter(char c) => c >= 'A' && c <= 'Z';
    private static bool IsUpperAlphanumeric(char c) => IsUpperLetter(c) || (c >= '0' && c <= '9');
}
