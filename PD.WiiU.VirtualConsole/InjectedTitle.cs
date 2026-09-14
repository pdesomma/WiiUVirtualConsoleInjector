using WiiUSharp;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Result of an injection: the packed title and its metadata.
/// </summary>
public sealed class InjectedTitle
{
    /// <summary>
    /// Creates a new instance of the <see cref="InjectedTitle"/> class.
    /// </summary>
    /// <param name="game">Metadata the title was given.</param>
    /// <param name="outputDirectory">Folder holding the packed title.</param>
    public InjectedTitle(Game game, string outputDirectory)
    {
        Game = game ?? throw new ArgumentNullException(nameof(game));
        OutputDirectory = outputDirectory ?? throw new ArgumentNullException(nameof(outputDirectory));
    }

    /// <summary>
    /// Metadata the title was given.
    /// </summary>
    public Game Game { get; }
    /// <summary>
    /// Folder holding the packed title.
    /// </summary>
    public string OutputDirectory { get; }

    /// <inheritdoc/>
    public override string ToString() => OutputDirectory;
}
