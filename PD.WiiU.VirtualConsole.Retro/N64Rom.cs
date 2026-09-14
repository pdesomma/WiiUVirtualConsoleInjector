namespace PD.WiiU.VirtualConsole.Retro;

/// <summary>
/// Puts an N64 ROM into the big-endian .z64 order the Virtual Console expects.
/// </summary>
public static class N64Rom
{
    /// <summary>
    /// Bytes that open a .z64 image.
    /// </summary>
    public static readonly byte[] BigEndianMagic = { 0x80, 0x37, 0x12, 0x40 };
    /// <summary>
    /// Bytes that open a byte-swapped .v64 image.
    /// </summary>
    public static readonly byte[] ByteSwappedMagic = { 0x37, 0x80, 0x40, 0x12 };
    /// <summary>
    /// Bytes that open a little-endian .n64 image.
    /// </summary>
    public static readonly byte[] LittleEndianMagic = { 0x40, 0x12, 0x37, 0x80 };

    /// <summary>
    /// Returns the ROM in .z64 order, swapping .v64 and .n64 input; a .z64 comes back as a copy.
    /// </summary>
    /// <param name="rom">ROM bytes in any of the three orders.</param>
    /// <exception cref="InvalidDataException">The first four bytes match none of the orders.</exception>
    public static byte[] ToBigEndian(byte[] rom)
    {
        if (rom is null)
            throw new ArgumentNullException(nameof(rom));
        if (rom.Length < 4)
            throw new InvalidDataException("ROM is too short to carry an N64 header.");

        var result = (byte[])rom.Clone();
        if (StartsWith(rom, BigEndianMagic))
            return result;
        if (StartsWith(rom, ByteSwappedMagic))
        {
            for (var i = 0; i + 1 < result.Length; i += 2)
                (result[i], result[i + 1]) = (result[i + 1], result[i]);
            return result;
        }
        if (StartsWith(rom, LittleEndianMagic))
        {
            for (var i = 0; i + 3 < result.Length; i += 4)
                (result[i], result[i + 1], result[i + 2], result[i + 3]) = (result[i + 3], result[i + 2], result[i + 1], result[i]);
            return result;
        }
        throw new InvalidDataException("Not an N64 ROM: expected a .z64, .v64 or .n64 header.");
    }

    private static bool StartsWith(byte[] rom, byte[] magic) =>
        rom.Length >= magic.Length && rom.Take(magic.Length).SequenceEqual(magic);
}
