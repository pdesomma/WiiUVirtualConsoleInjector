namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Game file to inject.
/// </summary>
public sealed class Rom
{
    /// <summary>
    /// Creates a new instance of the <see cref="Rom"/> class.
    /// </summary>
    /// <param name="path">File path.</param>
    /// <param name="console">Console the file is for.</param>
    public Rom(string path, SourceConsole console)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));

        Path = path;
        Console = console;
    }

    /// <summary>
    /// Console the file is for.
    /// </summary>
    public SourceConsole Console { get; }
    /// <summary>
    /// File path.
    /// </summary>
    public string Path { get; }

    /// <inheritdoc/>
    public override string ToString() => Path;
}
