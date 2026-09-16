using WiiUSharp;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Stock Virtual Console title used as the template for an injection.
/// </summary>
public sealed class BaseTitle
{
    /// <summary>
    /// Creates a new instance of the <see cref="BaseTitle"/> class.
    /// </summary>
    /// <param name="titleId">Title ID.</param>
    /// <param name="name">Display name.</param>
    /// <param name="region">Release region.</param>
    /// <param name="console">Console it emulates.</param>
    public BaseTitle(TitleId titleId, string name, Region region, SourceConsole console)
    {
        TitleId = titleId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Region = region;
        Console = console;
    }

    /// <summary>
    /// Console it emulates.
    /// </summary>
    public SourceConsole Console { get; }
    /// <summary>
    /// True for a base the user added rather than one from the bundled catalog.
    /// </summary>
    public bool IsCustom { get; init; }
    /// <summary>
    /// True for the base the community found works best for its console; it is picked first.
    /// </summary>
    public bool IsRecommended { get; init; }
    /// <summary>
    /// Display name.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// Release region.
    /// </summary>
    public Region Region { get; }
    /// <summary>
    /// Title ID.
    /// </summary>
    public TitleId TitleId { get; }

    /// <inheritdoc/>
    public override string ToString() => $"{Name} [{Region}]";
}
