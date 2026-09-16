using WiiUSharp;
using WiiUSharp.Nus;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// What the previous application left on this machine that is worth carrying over.
/// </summary>
public sealed class LegacyInstall
{
    /// <summary>
    /// Creates a new instance of the <see cref="LegacyInstall"/> class.
    /// </summary>
    /// <param name="location">Folder or file the install was recognised by.</param>
    public LegacyInstall(string location)
    {
        Location = location ?? throw new ArgumentNullException(nameof(location));
    }

    /// <summary>
    /// Stored bases that match the catalog, with the folder each sits in.
    /// </summary>
    public IReadOnlyList<LegacyBase> Bases { get; init; } = Array.Empty<LegacyBase>();
    /// <summary>
    /// The Wii U common key it held, or null.
    /// </summary>
    public CommonKey? CommonKey { get; init; }
    /// <summary>
    /// True when there is anything to import.
    /// </summary>
    public bool HasAnything => CommonKey is not null || TitleKeys.Count > 0 || Bases.Count > 0 || OutputFolder is not null;
    /// <summary>
    /// Folder or file the install was recognised by.
    /// </summary>
    public string Location { get; }
    /// <summary>
    /// Where it wrote finished titles, or null.
    /// </summary>
    public string? OutputFolder { get; init; }
    /// <summary>
    /// Warnings the user had turned off there.
    /// </summary>
    public IReadOnlyList<InjectionWarning> SuppressedWarnings { get; init; } = Array.Empty<InjectionWarning>();
    /// <summary>
    /// Title keys it had stored.
    /// </summary>
    public IReadOnlyList<LegacyTitleKey> TitleKeys { get; init; } = Array.Empty<LegacyTitleKey>();
}

/// <summary>
/// A base the previous application had downloaded.
/// </summary>
/// <param name="Base">Catalog entry it is.</param>
/// <param name="Folder">Its unpacked title folder.</param>
public sealed record LegacyBase(BaseTitle Base, string Folder);

/// <summary>
/// A title key the previous application had stored.
/// </summary>
/// <param name="TitleId">Which title.</param>
/// <param name="Key">The key as the ticket carries it.</param>
public sealed record LegacyTitleKey(TitleId TitleId, EncryptedTitleKey Key);
