using WiiUSharp;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Names the template an injection was built on, so a rebuild can find it again: a base's title ID or a core's id.
/// </summary>
public sealed class TemplateKey : IEquatable<TemplateKey>
{
    private TemplateKey(TitleId? baseTitleId, string? coreId)
    {
        BaseTitleId = baseTitleId;
        CoreId = coreId;
    }

    /// <summary>
    /// Title ID of the base, or null for a core.
    /// </summary>
    public TitleId? BaseTitleId { get; }
    /// <summary>
    /// libretro id of the core, or null for a base.
    /// </summary>
    public string? CoreId { get; }
    /// <summary>
    /// True when the key names a RetroArch core.
    /// </summary>
    public bool IsCore => CoreId is not null;

    /// <summary>
    /// The key for a base.
    /// </summary>
    /// <param name="titleId">Its title ID.</param>
    public static TemplateKey Base(TitleId titleId) => new(titleId, null);

    /// <summary>
    /// The key for a core.
    /// </summary>
    /// <param name="coreId">Its libretro id.</param>
    public static TemplateKey Core(string coreId)
    {
        if (string.IsNullOrWhiteSpace(coreId))
            throw new ArgumentException("Core id is required.", nameof(coreId));
        return new TemplateKey(null, coreId);
    }

    /// <summary>
    /// The key of a template.
    /// </summary>
    /// <param name="template">Base or core.</param>
    public static TemplateKey Of(ITitleTemplate template) => template switch
    {
        null => throw new ArgumentNullException(nameof(template)),
        BaseTitle @base => Base(@base.TitleId),
        RetroArchCore core => Core(core.Id),
        _ => throw new NotSupportedException($"No key for a {template.GetType().Name}."),
    };

    /// <inheritdoc/>
    public bool Equals(TemplateKey? other) =>
        other is not null && Nullable.Equals(BaseTitleId, other.BaseTitleId) && string.Equals(CoreId, other.CoreId, StringComparison.Ordinal);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as TemplateKey);

    /// <inheritdoc/>
    public override int GetHashCode() => IsCore ? StringComparer.Ordinal.GetHashCode(CoreId!) : BaseTitleId!.Value.GetHashCode();

    /// <inheritdoc/>
    public override string ToString() => CoreId ?? BaseTitleId!.Value.ToString();
}
