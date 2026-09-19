using PD.WiiU.VirtualConsole.Options;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Everything needed to turn a ROM into a Wii U title.
/// </summary>
public sealed class Injection
{
    private readonly IConsoleOptions? _options;

    /// <summary>
    /// Creates a new instance of the <see cref="Injection"/> class.
    /// </summary>
    /// <param name="template">What the title is built on: a base or a RetroArch core.</param>
    /// <param name="rom">File to inject.</param>
    /// <param name="game">Metadata for the produced title.</param>
    /// <exception cref="ArgumentException">ROM console differs from the template's.</exception>
    public Injection(ITitleTemplate template, Rom rom, Game game)
    {
        Template = template ?? throw new ArgumentNullException(nameof(template));
        Rom = rom ?? throw new ArgumentNullException(nameof(rom));
        Game = game ?? throw new ArgumentNullException(nameof(game));

        if (rom.Console != template.Console)
            throw new ArgumentException($"ROM is for {rom.Console} but the {Kind(template)} is for {template.Console}.", nameof(rom));
    }

    /// <summary>
    /// Replacement images.
    /// </summary>
    public Artwork Artwork { get; init; } = Artwork.None;
    /// <summary>
    /// The base the title is built on, or null when it runs a RetroArch core.
    /// </summary>
    public BaseTitle? Base => Template as BaseTitle;
    /// <summary>
    /// Audio to play at boot, or null to keep the template's.
    /// </summary>
    public string? BootSoundPath { get; init; }
    /// <summary>
    /// Console being emulated.
    /// </summary>
    public SourceConsole Console => Template.Console;
    /// <summary>
    /// The RetroArch core the title runs, or null when it is built on a base.
    /// </summary>
    public RetroArchCore? Core => Template as RetroArchCore;
    /// <summary>
    /// How the finished title is written out.
    /// </summary>
    public OutputFormat Format { get; init; } = OutputFormat.Wup;
    /// <summary>
    /// Metadata for the produced title.
    /// </summary>
    public Game Game { get; }
    /// <summary>
    /// Console-specific settings, or null for defaults.
    /// </summary>
    /// <exception cref="ArgumentException">Settings are for a different console.</exception>
    public IConsoleOptions? Options
    {
        get => _options;
        init
        {
            if (value is not null && value.Console != Console)
                throw new ArgumentException($"Options are for {value.Console} but the injection is for {Console}.", nameof(value));
            _options = value;
        }
    }
    /// <summary>
    /// File to inject.
    /// </summary>
    public Rom Rom { get; }
    /// <summary>
    /// What the title is built on.
    /// </summary>
    public ITitleTemplate Template { get; }

    /// <summary>
    /// The word for a template in messages.
    /// </summary>
    /// <param name="template">Template to name.</param>
    private static string Kind(ITitleTemplate template) => template is RetroArchCore ? "core" : "base";
}
