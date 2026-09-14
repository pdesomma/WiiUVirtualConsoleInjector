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
    /// <param name="base">Template title.</param>
    /// <param name="rom">File to inject.</param>
    /// <param name="game">Metadata for the produced title.</param>
    /// <exception cref="ArgumentException">ROM console differs from the base's.</exception>
    public Injection(BaseTitle @base, Rom rom, Game game)
    {
        Base = @base ?? throw new ArgumentNullException(nameof(@base));
        Rom = rom ?? throw new ArgumentNullException(nameof(rom));
        Game = game ?? throw new ArgumentNullException(nameof(game));

        if (rom.Console != @base.Console)
            throw new ArgumentException($"ROM is for {rom.Console} but the base is for {@base.Console}.", nameof(rom));
    }

    /// <summary>
    /// Replacement images.
    /// </summary>
    public Artwork Artwork { get; init; } = Artwork.None;
    /// <summary>
    /// Template title.
    /// </summary>
    public BaseTitle Base { get; }
    /// <summary>
    /// Audio to play at boot, or null to keep the base title's.
    /// </summary>
    public string? BootSoundPath { get; init; }
    /// <summary>
    /// Console being emulated.
    /// </summary>
    public SourceConsole Console => Base.Console;
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
}
