using PD.WiiU.VirtualConsole.Ports;

namespace PD.WiiU.VirtualConsole.Retro;

/// <summary>
/// Injects an MSX ROM: the base's content/msx/msx.pkg keeps its leading bytes and the ROM replaces everything after them.
/// </summary>
public sealed class MsxRomInjector : IRomInjector
{
    /// <summary>
    /// Bytes of the base package that precede the ROM, as measured on the known MSX base.
    /// </summary>
    public const int DefaultHeaderLength = 0x580B3;
    /// <summary>
    /// Folder under content holding the package.
    /// </summary>
    public const string EmulatorFolder = "msx";
    /// <summary>
    /// The package file.
    /// </summary>
    public const string PackageFileName = "msx.pkg";

    /// <summary>
    /// Creates a new instance of the <see cref="MsxRomInjector"/> class.
    /// </summary>
    /// <param name="headerLength">Bytes of the base package to keep before the ROM.</param>
    public MsxRomInjector(int headerLength = DefaultHeaderLength)
    {
        if (headerLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(headerLength));

        HeaderLength = headerLength;
    }

    /// <inheritdoc/>
    public SourceConsole Console => SourceConsole.Msx;
    /// <summary>
    /// Bytes of the base package kept before the ROM.
    /// </summary>
    public int HeaderLength { get; }

    /// <inheritdoc/>
    public IReadOnlyList<BaseIssue> Inspect(TitleDirectory title)
    {
        if (title is null)
            throw new ArgumentNullException(nameof(title));

        var inspection = new BaseInspection(title);
        inspection.RequireFile(TitleDirectory.ContentFolder + "/" + EmulatorFolder + "/" + PackageFileName, HeaderLength);
        return inspection.Issues;
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidDataException">The base package is shorter than the header.</exception>
    public Task InjectAsync(Injection injection, TitleDirectory title, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (injection is null)
            throw new ArgumentNullException(nameof(injection));
        if (title is null)
            throw new ArgumentNullException(nameof(title));
        if (injection.Console != SourceConsole.Msx)
            throw new ArgumentException($"Injection is for {injection.Console}.", nameof(injection));

        var package = Path.Combine(title.Content, EmulatorFolder, PackageFileName);
        if (!File.Exists(package))
            throw new FileNotFoundException("The base has no msx.pkg.", package);

        progress?.Report($"Injecting {Path.GetFileName(injection.Rom.Path)}");
        var rom = File.ReadAllBytes(injection.Rom.Path);
        cancellationToken.ThrowIfCancellationRequested();
        using var stream = new FileStream(package, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        if (stream.Length < HeaderLength)
            throw new InvalidDataException($"msx.pkg is {stream.Length} bytes; expected at least {HeaderLength}.");
        stream.SetLength(HeaderLength);
        stream.Position = HeaderLength;
        stream.Write(rom, 0, rom.Length);
        return Task.CompletedTask;
    }
}
