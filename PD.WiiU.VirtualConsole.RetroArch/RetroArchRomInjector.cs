using System.Text.RegularExpressions;
using PD.WiiU.VirtualConsole.Ports;

namespace PD.WiiU.VirtualConsole.RetroArch;

/// <summary>
/// Puts a ROM into a staged RetroArch core title: the file goes under content and the core is told to load it through cos.xml's argument string.
/// </summary>
public sealed class RetroArchRomInjector : IRomInjector
{
    /// <summary>
    /// Name given to a ROM whose own name has nothing usable in it.
    /// </summary>
    public const string FallbackStem = "game";

    private static readonly Regex Underscores = new("_+", RegexOptions.CultureInvariant);

    /// <summary>
    /// Creates a new instance of the <see cref="RetroArchRomInjector"/> class.
    /// </summary>
    /// <param name="console">Console this injector serves.</param>
    public RetroArchRomInjector(SourceConsole console)
    {
        Console = console;
    }

    /// <inheritdoc/>
    public SourceConsole Console { get; }

    /// <summary>
    /// The file name the ROM takes under content: its own, with anything the loader would split on or the console cannot store replaced by underscores. Saves on the card are named after it, so it stays recognisable.
    /// </summary>
    /// <param name="romPath">The ROM.</param>
    public static string ContentFileName(string romPath)
    {
        if (string.IsNullOrWhiteSpace(romPath))
            throw new ArgumentException("ROM path is required.", nameof(romPath));

        var stem = new string(Path.GetFileNameWithoutExtension(romPath).Select(c => IsSafe(c) ? c : '_').ToArray());
        stem = Underscores.Replace(stem, "_").Trim('_', '.');
        var extension = Path.GetExtension(romPath).ToLowerInvariant();
        return (stem.Length == 0 ? FallbackStem : stem) + extension;
    }

    /// <inheritdoc/>
    public IReadOnlyList<BaseIssue> Inspect(TitleDirectory title)
    {
        if (title is null)
            throw new ArgumentNullException(nameof(title));

        var inspection = new BaseInspection(title);
        inspection.RequireAny(TitleDirectory.CodeFolder, "*" + RetroArchCore.RpxSuffix);
        var cos = TitleDirectory.CodeFolder + "/" + CosXml.FileName;
        if (inspection.RequireFile(cos))
            inspection.Parse(cos, () => CosXml.Load(Path.Combine(title.Code, CosXml.FileName)));
        return inspection.Issues;
    }

    /// <inheritdoc/>
    /// <exception cref="FileNotFoundException">The title has no core executable.</exception>
    public Task InjectAsync(Injection injection, TitleDirectory title, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (injection is null)
            throw new ArgumentNullException(nameof(injection));
        if (title is null)
            throw new ArgumentNullException(nameof(title));
        if (injection.Console != Console)
            throw new ArgumentException($"Injection is for {injection.Console}.", nameof(injection));

        var rpx = Directory.GetFiles(title.Code, "*" + RetroArchCore.RpxSuffix).OrderBy(p => p, StringComparer.Ordinal).FirstOrDefault()
            ?? throw new FileNotFoundException($"No core executable in {title.Code}.");
        var fileName = ContentFileName(injection.Rom.Path);

        progress?.Report($"Copying {Path.GetFileName(injection.Rom.Path)} as {fileName}");
        File.Copy(injection.Rom.Path, Path.Combine(title.Content, fileName), overwrite: true);
        cancellationToken.ThrowIfCancellationRequested();

        progress?.Report("Pointing " + CosXml.FileName + " at it");
        var cosPath = Path.Combine(title.Code, CosXml.FileName);
        var cos = CosXml.Load(cosPath);
        cos.Arguments = Path.GetFileName(rpx) + " " + RetroArchTemplate.ContentMount + fileName;
        cos.Save(cosPath);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Letters, digits, dot, dash and underscore survive; spaces would split the argument string.
    /// </summary>
    private static bool IsSafe(char c) => c < 128 && (char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_');
}
