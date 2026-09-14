using System.Globalization;

namespace PD.WiiU;

/// <summary>
/// Groups a title with its updates and DLC. Written as eight upper-case hex digits,
/// e.g. <c>00001234</c>. Nintendo titles use the low 16 bits of the title's unique ID
/// and leave the high 16 bits zero.
/// </summary>
public readonly struct GroupId : IEquatable<GroupId>
{
    /// <summary>Number of hex digits in the textual form.</summary>
    public const int Length = 8;

    /// <summary>The raw 32-bit value.</summary>
    public uint Value { get; }

    public GroupId(uint value)
    {
        Value = value;
    }

    /// <summary>Parses the eight-hex-digit textual form.</summary>
    /// <exception cref="FormatException">The text is not exactly eight hex digits.</exception>
    public static GroupId Parse(string text)
    {
        if (!TryParse(text, out var id))
            throw new FormatException($"'{text}' is not a group ID; expected {Length} hex digits.");
        return id;
    }

    /// <summary>Parses the eight-hex-digit textual form without throwing.</summary>
    public static bool TryParse(string? text, out GroupId id)
    {
        id = default;
        if (text is null || text.Length != Length)
            return false;
        if (!uint.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value))
            return false;
        id = new GroupId(value);
        return true;
    }

    /// <summary>The eight upper-case hex digits the Wii U expects.</summary>
    public override string ToString() => Value.ToString("X8", CultureInfo.InvariantCulture);

    public bool Equals(GroupId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is GroupId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public static bool operator ==(GroupId left, GroupId right) => left.Equals(right);
    public static bool operator !=(GroupId left, GroupId right) => !left.Equals(right);
}
