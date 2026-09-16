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
    /// Font file the generated boot screens are captioned in; null to use whatever can be found.
    /// </summary>
    public string? CaptionFontPath { get; init; }
    /// <summary>
    /// Look for a newer release when the application starts.
    /// </summary>
    public bool CheckForUpdates { get; init; } = true;
    /// <summary>
    /// Copy each finished title onto the SD card's install folder.
    /// </summary>
    public bool CopyToSdCard { get; init; }
    /// <summary>
    /// When a release was last looked for; null when never.
    /// </summary>
    public DateTimeOffset? LastUpdateCheck { get; init; }
    /// <summary>
    /// The previous application's data has been pointed out once; the offer is not repeated.
    /// </summary>
    public bool LegacyImportOffered { get; init; }
    /// <summary>
    /// Folder injected titles are written to; null until chosen.
    /// </summary>
    public string? OutputPath { get; init; }
    /// <summary>
    /// Root of the SD card; null to use whichever drive is detected.
    /// </summary>
    public string? SdPath { get; init; }
    /// <summary>
    /// Warnings the user has asked not to see again.
    /// </summary>
    public IReadOnlyCollection<InjectionWarning> SuppressedWarnings
    {
        get => _suppressedWarnings;
        init => _suppressedWarnings = (value ?? throw new ArgumentNullException(nameof(value))).Distinct().ToArray();
    }
    /// <summary>
    /// Colour scheme; light unless the user chose otherwise.
    /// </summary>
    public AppTheme Theme { get; init; } = AppTheme.Light;
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
