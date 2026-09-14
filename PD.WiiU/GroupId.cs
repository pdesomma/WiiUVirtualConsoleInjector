using System.Globalization;

namespace PD.WiiU;

/// <summary>
/// Groups a title with its updates and DLC. Written as eight upper-case hex digits, e.g. <c>00001234</c>. Nintendo titles use the low 16 bits of the title's unique ID and leave the high 16 bits zero.
/// </summary>
public readonly struct GroupId : IEquatable<GroupId>
{
    /// <summary>
    /// Number of hex digits in the textual form.
    /// </summary>
    public const int Length = 8;

    /// <summary>
    /// Creates a new instance of the <see cref="GroupId"/> struct.
    /// </summary>
    /// <param name="value">The raw 32-bit value.</param>
    public GroupId(uint value)
    {
        Value = value;
    }

    /// <summary>
    /// The raw 32-bit value.
    /// </summary>
    public uint Value { get; }

    /// <inheritdoc/>
    public bool Equals(GroupId other) => Value == other.Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is GroupId other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(GroupId left, GroupId right) => left.Equals(right);

    public static bool operator !=(GroupId left, GroupId right) => !left.Equals(right);

    /// <summary>
    /// Parses the eight-hex-digit textual form.
    /// </summary>
    /// <param name="text">The eight hex digits to parse.</param>
    /// <exception cref="FormatException">The text is not exactly eight hex digits.</exception>
    public static GroupId Parse(string text)
    {
        if (!TryParse(text, out var id))
            throw new FormatException($"'{text}' is not a group ID; expected {Length} hex digits.");
        return id;
    }

    /// <summary>
    /// The eight upper-case hex digits the Wii U expects.
    /// </summary>
    public override string ToString() => Value.ToString("X8", CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses the eight-hex-digit textual form without throwing.
    /// </summary>
    /// <param name="text">The eight hex digits to parse.</param>
    /// <param name="id">The parsed group ID, or the default value when parsing fails.</param>
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
}
