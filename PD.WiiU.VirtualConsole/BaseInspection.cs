using WiiUSharp;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Collects what a base title is missing; each requirement adds an issue instead of throwing.
/// </summary>
public sealed class BaseInspection
{
    private readonly List<BaseIssue> _issues = new();

    /// <summary>
    /// Creates a new instance of the <see cref="BaseInspection"/> class.
    /// </summary>
    /// <param name="title">Title to inspect.</param>
    public BaseInspection(TitleDirectory title)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
    }

    /// <summary>
    /// Issues found so far.
    /// </summary>
    public IReadOnlyList<BaseIssue> Issues => _issues;
    /// <summary>
    /// True while nothing is wrong.
    /// </summary>
    public bool Passed => _issues.Count == 0;
    /// <summary>
    /// Title under inspection.
    /// </summary>
    public TitleDirectory Title { get; }

    /// <summary>
    /// Requires code, content and meta folders plus parseable app.xml and meta.xml.
    /// </summary>
    public BaseInspection Layout()
    {
        RequireDirectory(TitleDirectory.CodeFolder);
        RequireDirectory(TitleDirectory.ContentFolder);
        RequireDirectory(TitleDirectory.MetaFolder);
        if (RequireFile(Relative(Title.AppXmlPath)))
            Parse(Relative(Title.AppXmlPath), () => AppXml.Load(Title.AppXmlPath));
        if (RequireFile(Relative(Title.MetaXmlPath)))
            Parse(Relative(Title.MetaXmlPath), () => MetaXml.Load(Title.MetaXmlPath));
        return this;
    }

    /// <summary>
    /// Adds an issue when the condition fails.
    /// </summary>
    /// <param name="condition">Must hold.</param>
    /// <param name="path">Relative path the issue is about.</param>
    /// <param name="message">What is wrong.</param>
    public bool Require(bool condition, string path, string message)
    {
        if (!condition)
            _issues.Add(new BaseIssue(path, message));
        return condition;
    }

    /// <summary>
    /// Requires at least one file matching a pattern in a folder.
    /// </summary>
    /// <param name="directory">Relative folder.</param>
    /// <param name="pattern">Search pattern, e.g. *.rpx.</param>
    /// <returns>The first match in ordinal order, or null.</returns>
    public string? RequireAny(string directory, string pattern)
    {
        var full = Full(directory);
        var matches = Directory.Exists(full) ? Directory.GetFiles(full, pattern).OrderBy(p => p, StringComparer.Ordinal).ToArray() : Array.Empty<string>();
        Require(matches.Length > 0, directory, $"no {pattern} file");
        return matches.FirstOrDefault();
    }

    /// <summary>
    /// Requires a folder, optionally with something inside.
    /// </summary>
    /// <param name="path">Relative folder.</param>
    /// <param name="nonEmpty">Also require at least one entry.</param>
    public bool RequireDirectory(string path, bool nonEmpty = false)
    {
        var full = Full(path);
        if (!Require(Directory.Exists(full), path, "folder missing"))
            return false;
        return !nonEmpty || Require(Directory.EnumerateFileSystemEntries(full).Any(), path, "folder empty");
    }

    /// <summary>
    /// Requires a file, optionally of a minimum length.
    /// </summary>
    /// <param name="path">Relative file.</param>
    /// <param name="minimumLength">Smallest acceptable length.</param>
    public bool RequireFile(string path, long minimumLength = 0)
    {
        var full = Full(path);
        if (!Require(File.Exists(full), path, "file missing"))
            return false;
        var length = new FileInfo(full).Length;
        return Require(length >= minimumLength, path, $"{length} bytes, expected at least {minimumLength}");
    }

    /// <summary>
    /// Runs a check that may throw; the exception message becomes the issue.
    /// </summary>
    /// <param name="path">Relative path the check is about.</param>
    /// <param name="check">Throws when the file is unusable.</param>
    public bool Parse(string path, Action check)
    {
        if (check is null)
            throw new ArgumentNullException(nameof(check));

        try
        {
            check();
            return true;
        }
        catch (Exception e) when (e is IOException or InvalidDataException or FormatException or InvalidOperationException or System.Xml.XmlException)
        {
            _issues.Add(new BaseIssue(path, e.Message));
            return false;
        }
    }

    /// <summary>
    /// Path relative to the title root with forward slashes.
    /// </summary>
    /// <param name="fullPath">Path inside the title.</param>
    public string Relative(string fullPath)
    {
        if (fullPath is null)
            throw new ArgumentNullException(nameof(fullPath));

        var root = Title.Root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var relative = fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? fullPath.Substring(root.Length) : fullPath;
        return relative.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Replace('\\', '/');
    }

    private string Full(string relative) => Path.Combine(Title.Root, relative.Replace('/', Path.DirectorySeparatorChar));
}
