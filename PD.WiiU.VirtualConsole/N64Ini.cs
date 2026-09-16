using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// The per-game INI the N64 Virtual Console reads from content/config: the [RomOption] settings a user would set by hand, plus whatever other sections a hand-written file carries.
/// </summary>
public sealed class N64Ini
{
    /// <summary>
    /// RAM the console reports with the Expansion Pak.
    /// </summary>
    public const uint ExpansionRam = 0x800000;
    /// <summary>
    /// RAM the console reports without it.
    /// </summary>
    public const uint BaseRam = 0x400000;

    private static readonly Regex Section = new(@"^\s*\[(?<name>[^\]]+)\]\s*$", RegexOptions.CultureInvariant);
    private static readonly Regex Pair = new(@"^\s*(?<key>[A-Za-z0-9_]+)\s*=\s*(?<value>.*?)\s*$", RegexOptions.CultureInvariant);

    /// <summary>
    /// Save memory type; null leaves the emulator to detect it.
    /// </summary>
    public N64BackupType? BackupType { get; set; }
    /// <summary>
    /// Save memory size in bytes (512 or 2048 for EEPROM, 32768 for SRAM, 131072 for Flash); null leaves it to the emulator.
    /// </summary>
    public int? BackupSize { get; set; }
    /// <summary>
    /// First line of the file, after the semicolon; usually the game's name.
    /// </summary>
    public string? Comment { get; set; }
    /// <summary>
    /// False reports 4 MB of RAM, as a console without the Expansion Pak; null says nothing.
    /// </summary>
    public bool? ExpansionPak { get; set; }
    /// <summary>
    /// Any further sections, verbatim.
    /// </summary>
    public string Extra { get; set; } = "";
    /// <summary>
    /// Sync frames to the retrace; null says nothing.
    /// </summary>
    public bool? RetraceByVsync { get; set; }
    /// <summary>
    /// Run the RSP on its own core; null says nothing.
    /// </summary>
    public bool? RspMultiCore { get; set; }
    /// <summary>
    /// Pass rumble through; null says nothing.
    /// </summary>
    public bool? Rumble { get; set; }
    /// <summary>
    /// Drive timing off the timer; null says nothing.
    /// </summary>
    public bool? UseTimer { get; set; }

    /// <summary>
    /// Reads a file: the known [RomOption] keys into properties, everything else into <see cref="Extra"/>.
    /// </summary>
    /// <param name="text">File contents.</param>
    public static N64Ini Parse(string text)
    {
        if (text is null)
            throw new ArgumentNullException(nameof(text));

        var ini = new N64Ini();
        var extra = new StringBuilder();
        var extraRomOptions = new StringBuilder();
        string? section = null;
        var first = true;
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw.TrimEnd();
            if (first)
            {
                first = false;
                if (line.TrimStart().StartsWith(";", StringComparison.Ordinal))
                {
                    ini.Comment = line.TrimStart().Substring(1).Trim();
                    continue;
                }
            }

            var head = Section.Match(line);
            if (head.Success)
            {
                section = head.Groups["name"].Value;
                if (!IsRomOption(section))
                    extra.AppendLine(line);
                continue;
            }

            if (!IsRomOption(section))
            {
                if (section is not null || line.Trim().Length > 0)
                    extra.AppendLine(line);
                continue;
            }

            var pair = Pair.Match(line);
            if (!pair.Success || !ini.Take(pair.Groups["key"].Value, pair.Groups["value"].Value))
            {
                if (line.Trim().Length > 0)
                    extraRomOptions.AppendLine(line);
            }
        }

        if (extraRomOptions.Length > 0)
        {
            extra.Insert(0, "[RomOption]" + Environment.NewLine + extraRomOptions + Environment.NewLine);
        }
        ini.Extra = extra.ToString().Trim();
        return ini;
    }

    /// <summary>
    /// The file text: comment, [RomOption] with the set properties, then <see cref="Extra"/>.
    /// </summary>
    public string ToText()
    {
        var text = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(Comment))
            text.Append(';').AppendLine(Comment!.Trim());
        text.AppendLine("[RomOption]");
        if (BackupType is { } type)
            text.Append("BackupType = ").AppendLine(((int)type).ToString(CultureInfo.InvariantCulture));
        if (BackupSize is { } size)
            text.Append("BackupSize = ").AppendLine(size.ToString(CultureInfo.InvariantCulture));
        Flag(text, "RetraceByVsync", RetraceByVsync);
        Flag(text, "Rumble", Rumble);
        Flag(text, "UseTimer", UseTimer);
        Flag(text, "RSPMultiCore", RspMultiCore);
        if (ExpansionPak is { } pak)
            text.Append("RamSize = 0x").AppendLine((pak ? ExpansionRam : BaseRam).ToString("X6", CultureInfo.InvariantCulture));
        if (!string.IsNullOrWhiteSpace(Extra))
            text.AppendLine().AppendLine(Extra.Trim());
        return text.ToString();
    }

    private static void Flag(StringBuilder text, string key, bool? value)
    {
        if (value is { } flag)
            text.Append(key).Append(" = ").AppendLine(flag ? "1" : "0");
    }

    private static bool IsRomOption(string? section) => string.Equals(section, "RomOption", StringComparison.OrdinalIgnoreCase);

    private static bool? ParseFlag(string value) => value.Trim() switch { "1" => true, "0" => false, _ => null };

    /// <summary>
    /// Applies one [RomOption] key when it is one of ours.
    /// </summary>
    private bool Take(string key, string value)
    {
        switch (key)
        {
            case "BackupType":
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var type) || type < 0 || type > 3)
                    return false;
                BackupType = (N64BackupType)type;
                return true;
            case "BackupSize":
                if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var size))
                    return false;
                BackupSize = size;
                return true;
            case "RetraceByVsync":
                return (RetraceByVsync = ParseFlag(value)) is not null;
            case "Rumble":
                return (Rumble = ParseFlag(value)) is not null;
            case "UseTimer":
                return (UseTimer = ParseFlag(value)) is not null;
            case "RSPMultiCore":
                return (RspMultiCore = ParseFlag(value)) is not null;
            case "RamSize":
                var hex = value.Trim();
                if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    hex = hex.Substring(2);
                if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var ram))
                    return false;
                ExpansionPak = ram >= ExpansionRam;
                return true;
            default:
                return false;
        }
    }
}
