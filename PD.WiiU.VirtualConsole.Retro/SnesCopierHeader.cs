namespace PD.WiiU.VirtualConsole.Retro;

/// <summary>
/// The 512-byte header copier devices (Super Wild Card, Game Doctor, Pro Fighter) left on dumped SNES ROMs; the Virtual Console wants it gone.
/// </summary>
public static class SnesCopierHeader
{
    /// <summary>
    /// Size of the header in bytes.
    /// </summary>
    public const int Size = 512;

    /// <summary>
    /// True when the ROM carries a copier header: a real SNES ROM is a whole number of KiB, a headered one is 512 bytes over.
    /// </summary>
    /// <param name="rom">ROM file bytes.</param>
    public static bool IsPresent(byte[] rom)
    {
        if (rom is null)
            throw new ArgumentNullException(nameof(rom));

        return rom.Length > Size && rom.Length % 1024 == Size;
    }

    /// <summary>
    /// The ROM without its copier header; the same bytes when it has none.
    /// </summary>
    /// <param name="rom">ROM file bytes.</param>
    public static byte[] Strip(byte[] rom)
    {
        if (!IsPresent(rom))
            return rom;

        var bare = new byte[rom.Length - Size];
        Array.Copy(rom, Size, bare, 0, bare.Length);
        return bare;
    }
}
