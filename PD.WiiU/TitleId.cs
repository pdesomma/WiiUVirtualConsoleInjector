using System.Globalization;

namespace PD.WiiU;

/// <summary>
/// The 64-bit identifier the Wii U uses to tell titles apart. Written as sixteen upper-case hex digits, e.g. <c>0005000212345678</c>: the high half is the <see cref="TitleType"/>, the low half is unique within that type.
/// </summary>
public readonly struct TitleId : IEquatable<TitleId>
{
    /// <summary>
    /// Number of hex digits in the textual form.
    /// </summary>
    public const int Length = 16;

    /// <summary>
    /// Creates a new instance of the <see cref="TitleId"/> struct.
    /// </summary>
    /// <param name="type">The kind of title; becomes the high 32 bits.</param>
    /// <param name="uniqueId">Identifies the title within its type; becomes the low 32 bits.</param>
    public TitleId(TitleType type, uint uniqueId)
        : this(((ulong)(uint)type << 32) | uniqueId)
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="TitleId"/> struct.
    /// </summary>
    /// <param name="value">The raw 64-bit value.</param>
    public TitleId(ulong value)
    {
        Value = value;
    }

    /// <summary>
    /// The kind of title, taken from the high 32 bits.
    /// </summary>
    public TitleType Type => (TitleType)(uint)(Value >> 32);
    /// <summary>
    /// The low 32 bits; identifies the title within its <see cref="Type"/>.
    /// </summary>
    public uint UniqueId => (uint)Value;
    /// <summary>
    /// The raw 64-bit value.
    /// </summary>
    public ulong Value { get; }

    /// <inheritdoc/>
    public bool Equals(TitleId other) => Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is TitleId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(TitleId left, TitleId right) => left.Equals(right);

    public static bool operator !=(TitleId left, TitleId right) => !left.Equals(right);

    /// <summary>
    /// Parses the sixteen-hex-digit textual form.
    /// </summary>
    /// <param name="text">The sixteen hex digits to parse.</param>
    /// <exception cref="FormatException">The text is not exactly sixteen hex digits.</exception>
    public static TitleId Parse(string text)
    {
        if (!TryParse(text, out var id))
            throw new FormatException($"'{text}' is not a title ID; expected {Length} hex digits.");
        return id;
    }

    /// <summary>
    /// The sixteen upper-case hex digits the Wii U expects.
    /// </summary>
    public override string ToString() => Value.ToString("X16", CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses the sixteen-hex-digit textual form without throwing.
    /// </summary>
    /// <param name="text">The sixteen hex digits to parse.</param>
    /// <param name="id">The parsed title ID, or the default value when parsing fails.</param>
    public static bool TryParse(string? text, out TitleId id)
    {
        id = default;
        if (text is null || text.Length != Length)
            return false;
        if (!ulong.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value))
            return false;
        id = new TitleId(value);
        return true;
    }
}
