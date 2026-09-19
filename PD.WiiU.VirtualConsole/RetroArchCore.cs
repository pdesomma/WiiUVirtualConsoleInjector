namespace PD.WiiU.VirtualConsole;

/// <summary>
/// A libretro core built for Aroma, shipped with the injector; it becomes the title's executable.
/// </summary>
public sealed class RetroArchCore : ITitleTemplate
{
    /// <summary>
    /// What a core's executable name ends with.
    /// </summary>
    public const string RpxSuffix = "_libretro.rpx";

    /// <summary>
    /// Creates a new instance of the <see cref="RetroArchCore"/> class.
    /// </summary>
    /// <param name="id">libretro id, e.g. genesis_plus_gx.</param>
    /// <param name="name">Display name.</param>
    /// <param name="console">Console it emulates.</param>
    /// <param name="description">One line on what sets it apart.</param>
    public RetroArchCore(string id, string name, SourceConsole console, string description)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id is required.", nameof(id));
        if (id.Any(c => !(char.IsLetterOrDigit(c) || c == '_')))
            throw new ArgumentException("Id may only hold letters, digits and underscores.", nameof(id));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Id = id;
        Name = name;
        Console = console;
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    /// <inheritdoc/>
    public SourceConsole Console { get; }
    /// <summary>
    /// One line on what sets it apart.
    /// </summary>
    public string Description { get; }
    /// <summary>
    /// libretro id, e.g. genesis_plus_gx.
    /// </summary>
    public string Id { get; }
    /// <summary>
    /// True for the core to pick first for its console.
    /// </summary>
    public bool IsRecommended { get; init; }
    /// <inheritdoc/>
    public string Name { get; }
    /// <summary>
    /// File name of the executable, e.g. genesis_plus_gx_libretro.rpx.
    /// </summary>
    public string RpxFileName => Id + RpxSuffix;

    /// <inheritdoc/>
    public override string ToString() => Name;
}
