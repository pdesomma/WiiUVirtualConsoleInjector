using System.Globalization;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// The console's Starbuck ancast key; unlocks cafe2wii patching for Wii homebrew overclocking.
/// </summary>
public readonly struct AncastKey : IEquatable<AncastKey>
{
    /// <summary>
    /// Key length in bytes.
    /// </summary>
    public const int Size = 16;

    private readonly byte[] _bytes;

    /// <summary>
    /// Creates a new instance of the <see cref="AncastKey"/> struct.
    /// </summary>
    /// <param name="bytes">Sixteen key bytes.</param>
    /// <exception cref="ArgumentException">Not sixteen bytes.</exception>
    public AncastKey(byte[] bytes)
    {
        if (bytes is null)
            throw new ArgumentNullException(nameof(bytes));
        if (bytes.Length != Size)
            throw new ArgumentException($"Key must be {Size} bytes.", nameof(bytes));

        _bytes = (byte[])bytes.Clone();
    }

    /// <inheritdoc/>
    public bool Equals(AncastKey other) => ToArray().SequenceEqual(other.ToArray());

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is AncastKey other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => BitConverter.ToInt32(ToArray(), 0);

    public static bool operator ==(AncastKey left, AncastKey right) => left.Equals(right);

    public static bool operator !=(AncastKey left, AncastKey right) => !left.Equals(right);

    /// <summary>
    /// Parses thirty-two hex characters.
    /// </summary>
    /// <param name="hex">Key as hex.</param>
    /// <exception cref="FormatException">Not thirty-two hex characters.</exception>
    public static AncastKey Parse(string hex)
    {
        if (hex is null)
            throw new ArgumentNullException(nameof(hex));
        if (hex.Length != Size * 2)
            throw new FormatException($"Key must be {Size * 2} hex characters.");

        var bytes = new byte[Size];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = byte.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return new AncastKey(bytes);
    }

    /// <summary>
    /// Copy of the key bytes.
    /// </summary>
    public byte[] ToArray() => (byte[])(_bytes ?? new byte[Size]).Clone();

    /// <summary>
    /// Key as lowercase hex.
    /// </summary>
    public override string ToString() => string.Concat(ToArray().Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
}
