using System.Text.RegularExpressions;

namespace PD.WiiU.VirtualConsole.RetroArch;

/// <summary>
/// The files a cue sheet or m3u playlist points at, so a disc image travels with its tracks and a multi-disc set with its discs.
/// </summary>
public static class DiscReferences
{
    private const string CueExtension = ".cue";
    private const string M3uExtension = ".m3u";

    private static readonly Regex CueFileLine = new(@"^(?<lead>\s*FILE\s+)(?<name>""[^""]*""|\S+)(?<rest>.*)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex Lines = new(@"[^\r\n]+", RegexOptions.CultureInvariant);

    /// <summary>
    /// The text with every reference cut down to its file name; only cue FILE lines and m3u entries change, everything else stays byte for byte.
    /// </summary>
    /// <param name="text">Cue or m3u text.</param>
    /// <param name="extension">Which of the two it is, with the dot.</param>
    public static string Flatten(string text, string extension)
    {
        if (text is null)
            throw new ArgumentNullException(nameof(text));
        if (IsCue(extension))
            return Lines.Replace(text, line => FlattenCueLine(line.Value));
        if (IsM3u(extension))
            return Lines.Replace(text, line => FlattenM3uLine(line.Value));
        return text;
    }

    /// <summary>
    /// Full paths of every file the cue or m3u references, recursively, in first-seen order without duplicates; the file itself is left out. Anything else references nothing.
    /// </summary>
    /// <param name="path">Cue, m3u or any other file.</param>
    /// <exception cref="FileNotFoundException">A referenced file is not there.</exception>
    public static IReadOnlyList<string> Of(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));
        if (!Refers(path))
            return Array.Empty<string>();

        var root = Path.GetFullPath(path);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { root };
        var found = new List<string>();
        Collect(root, seen, found);
        return found;
    }

    /// <summary>
    /// True for a cue sheet or m3u playlist, the two that point at other files.
    /// </summary>
    /// <param name="path">File to test.</param>
    public static bool Refers(string path)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));
        var extension = Path.GetExtension(path);
        return IsCue(extension) || IsM3u(extension);
    }

    /// <summary>
    /// Adds what a file references, then what those reference in turn.
    /// </summary>
    /// <param name="file">Cue or m3u, full path.</param>
    /// <param name="seen">Every path met so far, the root included.</param>
    /// <param name="found">Where references land.</param>
    private static void Collect(string file, HashSet<string> seen, List<string> found)
    {
        var folder = Path.GetDirectoryName(file)!;
        foreach (var name in Names(file))
        {
            var resolved = Resolve(folder, name);
            if (!File.Exists(resolved))
                throw new FileNotFoundException($"{Path.GetFileName(file)} refers to {name}, which is not there.", resolved);
            if (!seen.Add(resolved))
                continue;
            found.Add(resolved);
            if (Refers(resolved))
                Collect(resolved, seen, found);
        }
    }

    /// <summary>
    /// The part after the last slash of either kind.
    /// </summary>
    /// <param name="name">A referenced name.</param>
    private static string FileName(string name)
    {
        var cut = name.LastIndexOfAny(new[] { '/', '\\' });
        return cut < 0 ? name : name.Substring(cut + 1);
    }

    /// <summary>
    /// A cue line with its FILE path cut to the name; other lines unchanged.
    /// </summary>
    /// <param name="line">One line, no line ending.</param>
    private static string FlattenCueLine(string line)
    {
        var match = CueFileLine.Match(line);
        if (!match.Success)
            return line;
        var name = match.Groups["name"].Value;
        var quoted = name.Length >= 2 && name[0] == '"' && name[name.Length - 1] == '"';
        var flat = FileName(Unquote(name));
        return match.Groups["lead"].Value + (quoted ? "\"" + flat + "\"" : flat) + match.Groups["rest"].Value;
    }

    /// <summary>
    /// An m3u entry cut to its file name, surrounding whitespace kept; blank and comment lines unchanged.
    /// </summary>
    /// <param name="line">One line, no line ending.</param>
    private static string FlattenM3uLine(string line)
    {
        var entry = line.Trim();
        if (entry.Length == 0 || entry[0] == '#')
            return line;
        var lead = line.Length - line.TrimStart().Length;
        var trail = line.Length - line.TrimEnd().Length;
        return line.Substring(0, lead) + FileName(entry) + line.Substring(line.Length - trail);
    }

    /// <summary>
    /// True for the cue extension, any case.
    /// </summary>
    /// <param name="extension">Extension with its dot.</param>
    private static bool IsCue(string? extension) => string.Equals(extension, CueExtension, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True for the m3u extension, any case.
    /// </summary>
    /// <param name="extension">Extension with its dot.</param>
    private static bool IsM3u(string? extension) => string.Equals(extension, M3uExtension, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The names a cue's FILE lines or an m3u's entries carry, as written.
    /// </summary>
    /// <param name="file">Cue or m3u.</param>
    private static IEnumerable<string> Names(string file)
    {
        var lines = File.ReadAllLines(file);
        if (IsCue(Path.GetExtension(file)))
            return lines.Select(line => CueFileLine.Match(line)).Where(match => match.Success).Select(match => Unquote(match.Groups["name"].Value));
        return lines.Select(line => line.Trim()).Where(entry => entry.Length > 0 && entry[0] != '#');
    }

    /// <summary>
    /// The name against the referencing file's folder, whichever slash it uses; an absolute name stands on its own.
    /// </summary>
    /// <param name="folder">Folder of the referencing file.</param>
    /// <param name="name">A referenced name.</param>
    private static string Resolve(string folder, string name) =>
        Path.GetFullPath(Path.Combine(folder, name.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)));

    /// <summary>
    /// The name without its surrounding quotes, when it has them.
    /// </summary>
    /// <param name="name">A cue FILE name as written.</param>
    private static string Unquote(string name) =>
        name.Length >= 2 && name[0] == '"' && name[name.Length - 1] == '"' ? name.Substring(1, name.Length - 2) : name;
}
