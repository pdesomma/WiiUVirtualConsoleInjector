using WiiUSharp.Nus;

namespace PD.WiiU.VirtualConsole.Infrastructure;

/// <summary>
/// Reads keys out of a Wii U otp.bin dump.
/// </summary>
public static class OtpFile
{
    /// <summary>
    /// Where the common key sits.
    /// </summary>
    public const int CommonKeyOffset = 0xE0;

    private const int KeySize = 16;

    /// <summary>
    /// Reads the common key from an otp.bin.
    /// </summary>
    /// <param name="path">otp.bin path.</param>
    /// <exception cref="InvalidDataException">The file ends before the common key does.</exception>
    public static CommonKey ReadCommonKey(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length < CommonKeyOffset + KeySize)
            throw new InvalidDataException($"{path} is {stream.Length} bytes; an otp.bin holds the common key at 0x{CommonKeyOffset:X}.");

        stream.Position = CommonKeyOffset;
        var bytes = new byte[KeySize];
        var read = 0;
        while (read < KeySize)
            read += stream.Read(bytes, read, KeySize - read);
        return new CommonKey(bytes);
    }
}
