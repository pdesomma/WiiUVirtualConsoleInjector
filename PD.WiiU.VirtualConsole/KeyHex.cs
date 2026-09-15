using System.Globalization;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Hex text for the keys users paste in.
/// </summary>
public static class KeyHex
{
    /// <summary>
    /// Lowercase hex of the bytes.
    /// </summary>
    /// <param name="bytes">Bytes to format.</param>
    public static string Format(byte[] bytes)
    {
        if (bytes is null)
            throw new ArgumentNullException(nameof(bytes));

        return string.Concat(bytes.Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// Parses hex of an exact byte length; surrounding whitespace is ignored.
    /// </summary>
    /// <param name="hex">Hex text.</param>
    /// <param name="size">Bytes expected.</param>
    /// <exception cref="FormatException">Wrong length or not hex.</exception>
    public static byte[] Parse(string hex, int size)
    {
        if (hex is null)
            throw new ArgumentNullException(nameof(hex));

        hex = hex.Trim();
        if (hex.Length != size * 2)
            throw new FormatException($"Key must be {size * 2} hex characters.");

        var bytes = new byte[size];
        for (var i = 0; i < size; i++)
            bytes[i] = byte.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return bytes;
    }
}
