namespace PD.WiiU.VirtualConsole;

/// <summary>
/// A console that RetroArch cores emulate: which ROM files it takes.
/// </summary>
public sealed class RetroArchSystem
{
    /// <summary>
    /// Creates a new instance of the <see cref="RetroArchSystem"/> class.
    /// </summary>
    /// <param name="console">The console.</param>
    /// <param name="extensions">ROM extensions with their dot, e.g. ".sms".</param>
    public RetroArchSystem(SourceConsole console, params string[] extensions)
    {
        if (extensions is null)
            throw new ArgumentNullException(nameof(extensions));
        if (extensions.Length == 0)
            throw new ArgumentException("At least one extension is required.", nameof(extensions));
        if (extensions.Any(e => string.IsNullOrWhiteSpace(e) || e[0] != '.' || e.Length < 2))
            throw new ArgumentException("Extensions start with a dot and name the type.", nameof(extensions));

        Console = console;
        Extensions = extensions.Select(e => e.ToLowerInvariant()).ToArray();
    }

    /// <summary>
    /// The console.
    /// </summary>
    public SourceConsole Console { get; }
    /// <summary>
    /// ROM extensions with their dot, lower case.
    /// </summary>
    public IReadOnlyList<string> Extensions { get; }

    /// <summary>
    /// True when the file's extension is one the system takes.
    /// </summary>
    /// <param name="path">ROM path.</param>
    public bool Accepts(string path)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));
        return Extensions.Contains(Path.GetExtension(path).ToLowerInvariant());
    }
}
