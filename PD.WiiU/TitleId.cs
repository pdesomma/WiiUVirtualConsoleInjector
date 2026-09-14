using System.Globalization;

namespace PD.WiiU;

/// <summary>
/// The 64-bit identifier the Wii U uses to tell titles apart. Written as sixteen upper-case
/// hex digits, e.g. <c>0005000212345678</c>: the high half is the <see cref="TitleType"/>,
/// the low half is unique within that type.
/// </summary>
public readonly struct TitleId : IEquatable<TitleId>
{
    /// <summary>Number of hex digits in the textual form.</summary>
    public const int Length = 16;

    /// <summary>The raw 64-bit value.</summary>
    public ulong Value { get; }

    /// <summary>The kind of title, taken from the high 32 bits.</summary>
    public TitleType Type => (TitleType)(uint)(Value >> 32);

    /// <summary>The low 32 bits; identifies the title within its <see cref="Type"/>.</summary>
    public uint UniqueId => (uint)Value;

    public TitleId(ulong value)
    {
        Value = value;
    }

    public TitleId(TitleType type, uint uniqueId)
        : this(((ulong)(uint)type << 32) | uniqueId)
    {
    }

    /// <summary>Parses the sixteen-hex-digit textual form.</summary>
    /// <exception cref="FormatException">The text is not exactly sixteen hex digits.</exception>
    public static TitleId Parse(string text)
    {
        if (!TryParse(text, out var id))
            throw new FormatException($"'{text}' is not a title ID; expected {Length} hex digits.");
        return id;
    }

    /// <summary>Parses the sixteen-hex-digit textual form without throwing.</summary>
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

    /// <summary>The sixteen upper-case hex digits the Wii U expects.</summary>
    public override string ToString() => Value.ToString("X16", CultureInfo.InvariantCulture);

    public bool Equals(TitleId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is TitleId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(TitleId left, TitleId right) => left.Equals(right);
    public static bool operator !=(TitleId left, TitleId right) => !left.Equals(right);
}
