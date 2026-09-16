using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Gba;
using PD.WiiU.VirtualConsole.Infrastructure;
using PD.WiiU.VirtualConsole.Ports;
using PD.WiiU.VirtualConsole.Retro;
using PD.WiiU.VirtualConsole.Wii;

namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// Default <see cref="IInjectionServiceFactory"/>: every injector, Skia images, NAudio sounds, the NUS packer.
/// </summary>
public sealed class InjectionServiceFactory : IInjectionServiceFactory
{
    /// <summary>
    /// Display name of the key Wii disc injects need.
    /// </summary>
    public const string WiiCommonKeyName = "Wii common key";
    /// <summary>
    /// Display name of the key every inject needs.
    /// </summary>
    public const string WiiUCommonKeyName = "Wii U common key";

    private readonly IKeyStore _keys;
    private readonly ISettingsService _settings;

    /// <summary>
    /// Creates a new instance of the <see cref="InjectionServiceFactory"/> class.
    /// </summary>
    /// <param name="settings">Where the base store lives.</param>
    /// <param name="keys">The user's keys.</param>
    public InjectionServiceFactory(ISettingsService settings, IKeyStore keys)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _keys = keys ?? throw new ArgumentNullException(nameof(keys));
    }

    /// <inheritdoc/>
    public IInjectionService Create()
    {
        var commonKey = _keys.CommonKey ?? throw new InvalidOperationException($"The {WiiUCommonKeyName} has not been supplied.");
        var injectors = new List<IRomInjector>
        {
            new NesRomInjector(),
            new SnesRomInjector(),
            new N64RomInjector(),
            new GbaRomInjector(),
            new NdsRomInjector(),
            new Tg16RomInjector(),
            new MsxRomInjector(),
            new GameCubeRomInjector(),
            // without the Wii common key it still serves NKit images, homebrew and channels; GameCube reads the NFS key from the base itself
            new WiiRomInjector(_keys.WiiCommonKey),
        };

        return new InjectionService(
            new DirectoryBaseStore(_settings.BasePath),
            injectors,
            new SkiaImageConverter(),
            new NAudioBootSoundConverter(),
            new NusTitlePacker(commonKey));
    }

    /// <inheritdoc/>
    public string? RomFit(BaseTitle @base, string romPath)
    {
        if (@base is null)
            throw new ArgumentNullException(nameof(@base));
        if (string.IsNullOrWhiteSpace(romPath))
            throw new ArgumentException("ROM path is required.", nameof(romPath));

        IRomCapacity? bounded = @base.Console switch
        {
            SourceConsole.Nes => new NesRomInjector(),
            SourceConsole.Snes => new SnesRomInjector(),
            _ => null,
        };
        if (bounded is null || !File.Exists(romPath))
            return null;
        try
        {
            var capacity = bounded.Capacity(new DirectoryBaseStore(_settings.BasePath).Locate(@base));
            var size = bounded.RomSize(romPath);
            return size > capacity ? $"{Path.GetFileName(romPath)} is {RomSlot.Kilobytes(size)}; this base holds {RomSlot.Kilobytes(capacity)}. {RomSlot.BiggerBaseHint}" : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> MissingKeys(SourceConsole console, string? romPath = null)
    {
        var missing = new List<string>();
        if (_keys.CommonKey is null)
            missing.Add(WiiUCommonKeyName);
        if (console == SourceConsole.Wii && _keys.WiiCommonKey is null && romPath is not null && WiiRomInjector.NeedsCommonKey(romPath))
            missing.Add(WiiCommonKeyName);
        return missing;
    }
}
