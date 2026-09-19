using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// The name a ROM carries in its own header, offered as the title's name.
/// </summary>
public static class RomNames
{
    private const int WbfsHeaderOffset = 0x200;
    private static readonly Regex Spaces = new(@"\s+", RegexOptions.CultureInvariant);

    /// <summary>
    /// Reads the internal name: the disc title for Wii and GameCube, the cartridge title for GBA, NDS, N64, Genesis, 32X, Atari 7800, Lynx and Virtual Boy, the internal name for SNES. NES, MSX and TurboGrafx carry none.
    /// </summary>
    /// <param name="console">Console the ROM is for.</param>
    /// <param name="romPath">The ROM.</param>
    /// <returns>Null when there is nothing readable.</returns>
    public static string? Suggest(SourceConsole console, string romPath)
    {
        if (romPath is null)
            throw new ArgumentNullException(nameof(romPath));
        if (!File.Exists(romPath))
            return null;

        var extension = Path.GetExtension(romPath);
        var raw = console switch
        {
            SourceConsole.Wii or SourceConsole.GameCube => Disc(romPath, extension),
            SourceConsole.Gba => string.Equals(extension, ".gba", StringComparison.OrdinalIgnoreCase) ? Ascii(Read(romPath, 0xA0, 12)) : GameBoy(romPath),
            SourceConsole.Nds => NintendoDs(romPath),
            SourceConsole.N64 => Nintendo64(romPath),
            SourceConsole.Snes => SuperNintendo(romPath),
            SourceConsole.Genesis or SourceConsole.Sega32X => Genesis(romPath),
            SourceConsole.Atari7800 => Atari7800(romPath),
            SourceConsole.AtariLynx => Lynx(romPath),
            SourceConsole.VirtualBoy => VirtualBoy(romPath),
            _ => null,
        };
        return Tidy(raw);
    }

    /// <summary>
    /// Trims, collapses spaces and turns a shouting header into title case.
    /// </summary>
    /// <param name="raw">Header text.</param>
    public static string? Tidy(string? raw)
    {
        if (raw is null)
            return null;
        var text = Spaces.Replace(raw.Replace("\u2122", "").Replace("\u00AE", "").Replace("\u00A9", "").Trim(), " ");
        if (text.Length == 0)
            return null;
        if (text.Any(char.IsLetter) && !text.Any(char.IsLower))
            text = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.ToLowerInvariant());
        return text;
    }

    /// <summary>
    /// The 32-byte title at 0x11 of an A78 header, which announces itself with "ATARI7800" at 0x01.
    /// </summary>
    private static string? Atari7800(string path) =>
        Ascii(Read(path, 0x01, 9)) == "ATARI7800" ? Padded(Read(path, 0x11, 32)) : null;

    private static string? Ascii(byte[]? bytes)
    {
        if (bytes is null)
            return null;
        var end = Array.IndexOf(bytes, (byte)0);
        var length = end < 0 ? bytes.Length : end;
        return bytes.Take(length).All(b => b >= 0x20 && b < 0x7F) ? Encoding.ASCII.GetString(bytes, 0, length) : null;
    }

    private static string? Disc(string path, string extension)
    {
        if (string.Equals(extension, ".gcz", StringComparison.OrdinalIgnoreCase))
            return null;
        var offset = string.Equals(extension, ".wbfs", StringComparison.OrdinalIgnoreCase) ? WbfsHeaderOffset : 0;
        return Ascii(Read(path, offset + 0x20, 0x40));
    }

    /// <summary>
    /// The overseas name at 0x150 of a plain (non-interleaved) dump, falling back to the domestic one at 0x120; both are space-padded to 48 bytes.
    /// </summary>
    private static string? Genesis(string path)
    {
        if (Ascii(Read(path, 0x100, 4)) is not { } console || !console.StartsWith("SEGA", StringComparison.Ordinal))
            return null;
        return Padded(Read(path, 0x150, 48)) ?? Padded(Read(path, 0x120, 48));
    }

    /// <summary>
    /// Game Boy titles sit at 0x134, 16 bytes at most, with the CGB flag taking the last one.
    /// </summary>
    private static string? GameBoy(string path) => Ascii(Read(path, 0x134, 15));

    /// <summary>
    /// The 32-byte cartridge name at 0x0A of an LNX header, which starts with "LYNX".
    /// </summary>
    private static string? Lynx(string path) =>
        Ascii(Read(path, 0, 4)) == "LYNX" ? Padded(Read(path, 0x0A, 32)) : null;

    /// <summary>
    /// The banner's English title (its first line), which reads like the box; the twelve-character header code only as a fallback.
    /// </summary>
    private static string? NintendoDs(string path)
    {
        var header = Read(path, 0x68, 4);
        if (header is not null)
        {
            var banner = header[0] | header[1] << 8 | header[2] << 16 | header[3] << 24;
            var title = banner == 0 ? null : Read(path, (uint)banner + 0x340, 0x100);
            if (title is not null)
            {
                var text = Encoding.Unicode.GetString(title);
                var end = text.IndexOfAny(new[] { (char)0, (char)10 });
                var line = (end < 0 ? text : text.Substring(0, end)).Trim();
                if (line.Length > 0 && line.All(c => !char.IsControl(c)))
                    return line;
            }
        }
        return Ascii(Read(path, 0, 12));
    }

    /// <summary>
    /// The name at 0x20 in whatever byte order the dump uses, told by the magic word.
    /// </summary>
    private static string? Nintendo64(string path)
    {
        var head = Read(path, 0, 0x40);
        if (head is null)
            return null;
        var name = new byte[20];
        Array.Copy(head, 0x20, name, 0, 20);
        switch (head[0])
        {
            case 0x80:
                break;
            case 0x37:
                for (var i = 0; i < 20; i += 2)
                    (name[i], name[i + 1]) = (name[i + 1], name[i]);
                break;
            case 0x40:
                for (var i = 0; i < 20; i += 4)
                    (name[i], name[i + 1], name[i + 2], name[i + 3]) = (name[i + 3], name[i + 2], name[i + 1], name[i]);
                break;
            default:
                return null;
        }
        return Ascii(name);
    }

    /// <summary>
    /// A space-padded ASCII field; null when empty or not printable.
    /// </summary>
    private static string? Padded(byte[]? bytes)
    {
        var text = Ascii(bytes)?.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    /// <summary>
    /// The 20-byte title at the start of the header that sits 0x220 bytes before the end of the ROM.
    /// </summary>
    private static string? VirtualBoy(string path)
    {
        var length = new FileInfo(path).Length;
        return length < 0x220 ? null : Padded(Read(path, length - 0x220, 20));
    }

    private static byte[]? Read(string path, long offset, int count)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length < offset + count)
            return null;
        var bytes = new byte[count];
        stream.Position = offset;
        var read = 0;
        while (read < count)
        {
            var n = stream.Read(bytes, read, count - read);
            if (n == 0)
                return null;
            read += n;
        }
        return bytes;
    }

    /// <summary>
    /// LoROM or HiROM name, whichever header checks out, past a copier header if there is one.
    /// </summary>
    private static string? SuperNintendo(string path)
    {
        var length = new FileInfo(path).Length;
        var skip = length % 1024 == 512 ? 512 : 0;
        foreach (var header in new long[] { 0x7FC0, 0xFFC0 })
        {
            var bytes = Read(path, skip + header, 0x20);
            if (bytes is null)
                continue;
            // the checksum and its complement at 0x1C-0x1F must add up
            var complement = bytes[0x1C] | bytes[0x1D] << 8;
            var checksum = bytes[0x1E] | bytes[0x1F] << 8;
            if ((complement ^ checksum) != 0xFFFF)
                continue;
            var name = Ascii(bytes.Take(21).ToArray());
            if (!string.IsNullOrWhiteSpace(name))
                return name;
        }
        return null;
    }
}
