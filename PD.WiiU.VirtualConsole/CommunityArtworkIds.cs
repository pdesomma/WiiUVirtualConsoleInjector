using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// The folder names the community artwork repository files a ROM under: the game code from the ROM header where there is one, otherwise a four-letter code the previous application derived from a hash, reproduced here quirk for quirk so the folders still match.
/// </summary>
public static class CommunityArtworkIds
{
    private static readonly Regex NotAlphanumeric = new("[^a-zA-Z0-9 -]", RegexOptions.CultureInvariant);
    private static readonly Regex NotDigit = new("[^0-9]", RegexOptions.CultureInvariant);

    /// <summary>
    /// Repository paths to try, most likely first, e.g. "n64/NGEE".
    /// </summary>
    /// <param name="console">Console the ROM is for.</param>
    /// <param name="romPath">The ROM.</param>
    /// <returns>Empty when the ROM carries nothing to look up by.</returns>
    public static IReadOnlyList<string> Candidates(SourceConsole console, string romPath)
    {
        if (romPath is null)
            throw new ArgumentNullException(nameof(romPath));

        var ids = console switch
        {
            SourceConsole.Nes => Single(Nes(romPath)),
            SourceConsole.Snes => Single(Snes(romPath)),
            SourceConsole.Msx => Single(Hashed(romPath, "SX")),
            SourceConsole.Tg16 => Single(Hashed(romPath, "TG")),
            SourceConsole.Gba => IsGba(romPath) ? WithRegions(HeaderCode(romPath, 0xAC, 4)) : Array.Empty<string>(),
            // Game Boy carts carry no game code; the gba folder only ever held GBA codes
            SourceConsole.GameBoy => Array.Empty<string>(),
            SourceConsole.N64 => WithSwap(HeaderCode(romPath, 0x3A, 6)),
            SourceConsole.Nds => WithRegions(HeaderCode(romPath, 0xC, 4)),
            SourceConsole.Wii => WithDiscRegions(DiscCode(romPath)),
            SourceConsole.GameCube => WithDiscRegions(DiscCode(romPath)),
            _ => Array.Empty<string>(),
        };
        if (ids.Count == 0)
            return Array.Empty<string>();
        var folder = Folder(console);
        return ids.Where(id => id.Length > 0).Distinct(StringComparer.Ordinal).Select(id => folder + "/" + id).ToList();
    }

    /// <summary>
    /// Repository folder for a console; Game Boy shares the GBA one.
    /// </summary>
    /// <param name="console">Console.</param>
    public static string Folder(SourceConsole console) => console switch
    {
        SourceConsole.Nes => "nes",
        SourceConsole.Snes => "snes",
        SourceConsole.N64 => "n64",
        SourceConsole.Gba => "gba",
        SourceConsole.GameBoy => "gba",
        SourceConsole.Nds => "nds",
        SourceConsole.Tg16 => "tg16",
        SourceConsole.Msx => "msx",
        SourceConsole.Wii => "wii",
        SourceConsole.GameCube => "gcn",
        _ => throw new ArgumentOutOfRangeException(nameof(console), console, "Unknown console."),
    };

    /// <summary>
    /// Four letters from a number: pairs of its digits summed as characters, folded into the first 24 letters. The previous application's exact arithmetic.
    /// </summary>
    /// <param name="number">Seven to nine digits.</param>
    /// <exception cref="ArgumentOutOfRangeException">Fewer than seven digits.</exception>
    public static string LettersFor(int number)
    {
        var text = number.ToString(CultureInfo.InvariantCulture);
        if (number < 1000000)
            throw new ArgumentOutOfRangeException(nameof(number), number, "Needs at least seven digits.");
        var values = new[] { text[0] + text[1], text[2] + text[3], text[4] + text[5], text.Length > 7 ? text[6] + text[7] : text[6] };
        var letters = new char[4];
        for (var i = 0; i < 4; i++)
        {
            var value = values[i];
            while (value > 23)
                value -= 23;
            letters[i] = (char)('A' + value);
        }
        return new string(letters);
    }

    /// <summary>
    /// The nine digits of a hex digest, zero-padded, turned into letters.
    /// </summary>
    private static string CodeOf(byte[] digest)
    {
        var hex = string.Concat(digest.Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
        var digits = NotDigit.Replace(hex, "");
        while (digits.Length < 10)
            digits += "0";
        // leading zeros drop out of the number; the previous application had no code for so short a value
        var number = int.Parse(digits.Substring(0, 9), CultureInfo.InvariantCulture);
        return number < 1000000 ? "" : LettersFor(number);
    }

    private static string DiscCode(string path)
    {
        var wbfs = string.Equals(Path.GetExtension(path), ".wbfs", StringComparison.OrdinalIgnoreCase);
        return HeaderCode(path, wbfs ? 0x200 : 0, 6);
    }

    /// <summary>
    /// A code over the first 0x210 bytes, suffixed to say which console it belongs to.
    /// </summary>
    private static string Hashed(string path, string suffix)
    {
        var head = Read(path, 0, 0x210);
        if (head is null)
            return "";
        using var md5 = MD5.Create();
        return CodeOf(md5.ComputeHash(head)) + suffix;
    }

    private static string HeaderCode(string path, long offset, int length)
    {
        var bytes = Read(path, offset, length);
        return bytes is null ? "" : NotAlphanumeric.Replace(Encoding.ASCII.GetString(bytes), "");
    }

    private static bool IsGba(string path) => string.Equals(Path.GetExtension(path), ".gba", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// A code over 0xB0 bytes at 0x8000, where NES banks tend to differ.
    /// </summary>
    private static string Nes(string path)
    {
        var chunk = Read(path, 0x8000, 0xB0);
        if (chunk is null)
            return "";
        using var md5 = MD5.Create();
        return CodeOf(md5.ComputeHash(chunk));
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

    private static IReadOnlyList<string> Single(string id) => new[] { id };

    /// <summary>
    /// A code over the internal name: the HiROM name when the LoROM code is missing, the LoROM name when both are.
    /// </summary>
    private static string Snes(string path)
    {
        var code = HeaderCode(path, 0x7FB2, 4);
        var name = Array.Empty<byte>();
        if (code.Length < 4)
        {
            name = Read(path, 0xFFC0, 21) ?? Array.Empty<byte>();
            code = NotAlphanumeric.Replace(Encoding.ASCII.GetString(name), "");
        }
        if (code.Length < 4)
            name = Read(path, 0x7FC0, 21) ?? Array.Empty<byte>();
        var text = NotAlphanumeric.Replace(Encoding.ASCII.GetString(name), "");
        using var md5 = MD5.Create();
        return CodeOf(md5.ComputeHash(Encoding.UTF8.GetBytes(text)));
    }

    private static IReadOnlyList<string> WithDiscRegions(string id) =>
        id.Length < 6 ? Single(id) : new[] { id, id.Substring(0, 3) + "E" + id.Substring(4, 2), id.Substring(0, 3) + "P" + id.Substring(4, 2), id.Substring(0, 3) + "J" + id.Substring(4, 2) };

    private static IReadOnlyList<string> WithRegions(string id) =>
        id.Length < 4 ? Single(id) : new[] { id, id.Substring(0, 3) + "E", id.Substring(0, 3) + "P", id.Substring(0, 3) + "J" };

    /// <summary>
    /// The code as read plus its byte-swapped twin, since .v64 ROMs store it swapped.
    /// </summary>
    private static IReadOnlyList<string> WithSwap(string id) =>
        id.Length < 4 ? Single(id) : new[] { id, new string(new[] { id[0], id[2], id[1], id[3] }) };
}
