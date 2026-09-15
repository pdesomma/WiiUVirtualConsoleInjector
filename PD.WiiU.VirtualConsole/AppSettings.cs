namespace PD.WiiU.VirtualConsole;

/// <summary>
/// What the application remembers between runs. Keys live elsewhere; see <see cref="Ports.IKeyStore"/>.
/// </summary>
public sealed record AppSettings
{
    private readonly IReadOnlyCollection<InjectionWarning> _suppressedWarnings = Array.Empty<InjectionWarning>();

    /// <summary>
    /// Folder the base store lives in; null until chosen.
    /// </summary>
    public string? BasePath { get; init; }
    /// <summary>
    /// Folder injected titles are written to; null until chosen.
    /// </summary>
    public string? OutputPath { get; init; }
    /// <summary>
    /// Warnings the user has asked not to see again.
    /// </summary>
    public IReadOnlyCollection<InjectionWarning> SuppressedWarnings
    {
        get => _suppressedWarnings;
        init => _suppressedWarnings = (value ?? throw new ArgumentNullException(nameof(value))).Distinct().ToArray();
    }
    /// <summary>
    /// Scratch folder for staging; null to use the system temp folder.
    /// </summary>
    public string? WorkPath { get; init; }

    /// <summary>
    /// True when the user has silenced a warning.
    /// </summary>
    /// <param name="warning">Warning to check.</param>
    public bool IsSuppressed(InjectionWarning warning) => _suppressedWarnings.Contains(warning);

    /// <summary>
    /// A copy with a warning shown again.
    /// </summary>
    /// <param name="warning">Warning to restore.</param>
    public AppSettings Restore(InjectionWarning warning) =>
        this with { SuppressedWarnings = _suppressedWarnings.Where(w => w != warning).ToArray() };

    /// <summary>
    /// A copy with a warning silenced.
    /// </summary>
    /// <param name="warning">Warning to silence.</param>
    public AppSettings Suppress(InjectionWarning warning) =>
        this with { SuppressedWarnings = _suppressedWarnings.Append(warning).ToArray() };
}
