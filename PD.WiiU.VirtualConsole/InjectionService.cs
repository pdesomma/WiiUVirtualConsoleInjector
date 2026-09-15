using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Sequences the injection steps over the supplied ports.
/// </summary>
public sealed class InjectionService : IInjectionService
{
    /// <summary>
    /// What a Loadiine title's folder name starts with.
    /// </summary>
    public const string LoadiinePrefix = "[LOADIINE]";
    /// <summary>
    /// What every packed title's folder name starts with.
    /// </summary>
    public const string TitlePrefix = "[WUP]";

    private readonly IBaseStore _bases;
    private readonly IBootSoundConverter _bootSounds;
    private readonly IImageConverter _images;
    private readonly IReadOnlyDictionary<SourceConsole, IRomInjector> _injectors;
    private readonly ITitlePacker _packer;

    /// <summary>
    /// Creates a new instance of the <see cref="InjectionService"/> class.
    /// </summary>
    /// <param name="bases">Where bases come from.</param>
    /// <param name="injectors">One injector per console.</param>
    /// <param name="images">Artwork converter.</param>
    /// <param name="bootSounds">Boot sound converter.</param>
    /// <param name="packer">Final packer.</param>
    /// <exception cref="ArgumentException">Two injectors claim the same console.</exception>
    public InjectionService(IBaseStore bases, IEnumerable<IRomInjector> injectors, IImageConverter images, IBootSoundConverter bootSounds, ITitlePacker packer)
    {
        _bases = bases ?? throw new ArgumentNullException(nameof(bases));
        _images = images ?? throw new ArgumentNullException(nameof(images));
        _bootSounds = bootSounds ?? throw new ArgumentNullException(nameof(bootSounds));
        _packer = packer ?? throw new ArgumentNullException(nameof(packer));

        if (injectors is null)
            throw new ArgumentNullException(nameof(injectors));
        var byConsole = new Dictionary<SourceConsole, IRomInjector>();
        foreach (var injector in injectors)
        {
            if (byConsole.ContainsKey(injector.Console))
                throw new ArgumentException($"More than one injector for {injector.Console}.", nameof(injectors));
            byConsole.Add(injector.Console, injector);
        }
        _injectors = byConsole;
    }

    /// <summary>
    /// Consoles an injector was supplied for.
    /// </summary>
    public IReadOnlyCollection<SourceConsole> SupportedConsoles => _injectors.Keys.ToArray();

    /// <inheritdoc/>
    public async Task<InjectedTitle> InjectAsync(Injection injection, string workDirectory, string outputDirectory, IProgress<InjectionProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (injection is null)
            throw new ArgumentNullException(nameof(injection));
        if (string.IsNullOrWhiteSpace(workDirectory))
            throw new ArgumentException("Work directory is required.", nameof(workDirectory));
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory is required.", nameof(outputDirectory));
        if (!_injectors.TryGetValue(injection.Console, out var injector))
            throw new NotSupportedException($"No injector for {injection.Console}.");

        var work = Path.Combine(workDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            var title = await Run(InjectionStep.StageBase, $"Staging {injection.Base}", progress,
                () => _bases.StageAsync(injection.Base, work, cancellationToken)).ConfigureAwait(false);

            await Run(InjectionStep.InspectBase, "Checking base", progress,
                () => RequireUsable(Inspect(title, injector))).ConfigureAwait(false);

            await Run(InjectionStep.InjectRom, $"Injecting {Path.GetFileName(injection.Rom.Path)}", progress,
                () => injector.InjectAsync(injection, title, Detail(InjectionStep.InjectRom, progress), cancellationToken)).ConfigureAwait(false);

            await Run(InjectionStep.WriteMetadata, "Writing meta.xml and app.xml", progress,
                () => WriteMetadata(injection.Game, title)).ConfigureAwait(false);

            await Run(InjectionStep.ConvertArtwork, "Converting artwork", progress,
                () => ConvertArtwork(injection.Artwork, title, Detail(InjectionStep.ConvertArtwork, progress), cancellationToken)).ConfigureAwait(false);

            if (injection.BootSoundPath is { } sound)
                await Run(InjectionStep.ConvertBootSound, $"Converting {Path.GetFileName(sound)}", progress,
                    () => _bootSounds.ConvertAsync(sound, Path.Combine(title.Meta, BootSound.FileName), cancellationToken)).ConfigureAwait(false);

            var folder = TitleFolder(injection.Game, outputDirectory, injection.Format);
            Directory.CreateDirectory(folder);
            if (injection.Format == OutputFormat.Loadiine)
                await Run(InjectionStep.Pack, "Copying for Loadiine", progress,
                    () => CopyTree(title.Root, folder, cancellationToken)).ConfigureAwait(false);
            else
                await Run(InjectionStep.Pack, "Packing", progress,
                    () => _packer.PackAsync(title, folder, Detail(InjectionStep.Pack, progress), cancellationToken)).ConfigureAwait(false);

            return new InjectedTitle(injection.Game, folder) { IconTga = ReadIcon(title) };
        }
        finally
        {
            if (Directory.Exists(work))
                Directory.Delete(work, recursive: true);
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<BaseIssue> InspectBase(BaseTitle @base)
    {
        if (@base is null)
            throw new ArgumentNullException(nameof(@base));
        if (!_injectors.TryGetValue(@base.Console, out var injector))
            throw new NotSupportedException($"No injector for {@base.Console}.");

        return Inspect(_bases.Locate(@base), injector);
    }

    private static IReadOnlyList<BaseIssue> Inspect(TitleDirectory title, IRomInjector injector)
    {
        var layout = new BaseInspection(title).Layout();
        return layout.Passed ? injector.Inspect(title) : layout.Issues;
    }

    /// <summary>
    /// The staged icon, read before the work folder goes.
    /// </summary>
    /// <param name="title">Staged title.</param>
    private static byte[]? ReadIcon(TitleDirectory title)
    {
        var path = Path.Combine(title.Meta, ImageSlot.Icon.FileName);
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }

    private static Task RequireUsable(IReadOnlyList<BaseIssue> issues)
    {
        if (issues.Count > 0)
            throw new InvalidDataException("Base is not usable: " + string.Join("; ", issues));
        return Task.CompletedTask;
    }

    private async Task ConvertArtwork(Artwork artwork, TitleDirectory title, IProgress<string> progress, CancellationToken cancellationToken)
    {
        foreach (var slot in ImageSlot.All)
        {
            if (artwork.PathFor(slot) is not { } source)
                continue;
            progress.Report($"{slot.FileName} from {Path.GetFileName(source)}");
            await _images.ConvertAsync(source, slot, Path.Combine(title.Meta, slot.FileName), cancellationToken).ConfigureAwait(false);
        }
    }

    private static IProgress<string> Detail(InjectionStep step, IProgress<InjectionProgress>? progress) =>
        new StepProgress(step, progress);

    private static async Task Run(InjectionStep step, string message, IProgress<InjectionProgress>? progress, Func<Task> action)
    {
        await Run(step, message, progress, async () =>
        {
            await action().ConfigureAwait(false);
            return true;
        }).ConfigureAwait(false);
    }

    private static async Task<T> Run<T>(InjectionStep step, string message, IProgress<InjectionProgress>? progress, Func<Task<T>> action)
    {
        progress?.Report(new InjectionProgress(step, message));
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (InjectionException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new InjectionException(step, $"{message}: {e.Message}", e);
        }
    }

    private static Task WriteMetadata(Game game, TitleDirectory title)
    {
        var meta = MetaXml.Load(title.MetaXmlPath);
        meta.Apply(game);
        meta.Save(title.MetaXmlPath);

        var app = AppXml.Load(title.AppXmlPath);
        app.Apply(game);
        app.Save(title.AppXmlPath);
        return Task.CompletedTask;
    }

    private sealed class StepProgress : IProgress<string>
    {
        private readonly IProgress<InjectionProgress>? _progress;
        private readonly InjectionStep _step;

        public StepProgress(InjectionStep step, IProgress<InjectionProgress>? progress)
        {
            _step = step;
            _progress = progress;
        }

        public void Report(string value) => _progress?.Report(new InjectionProgress(_step, value));
    }
    /// <summary>
    /// Filename-safe form of a name, or null when nothing is left of it.
    /// </summary>
    /// <param name="name">Name as the user gave it.</param>
    private static string? Sanitize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name!.Select(c => invalid.Contains(c) ? ' ' : c).ToArray());
        cleaned = string.Join(" ", cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        return cleaned.Length == 0 ? null : cleaned;
    }

    /// <summary>
    /// Copies the unpacked title as it is, folder for folder.
    /// </summary>
    /// <param name="source">Unpacked title root.</param>
    /// <param name="destination">Folder to fill.</param>
    /// <param name="cancellationToken">Stops the copy.</param>
    private static Task CopyTree(string source, string destination, CancellationToken cancellationToken)
    {
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var target = Path.Combine(destination, file.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// The title's own folder under the output root, numbered when one of that name already holds files.
    /// </summary>
    /// <param name="game">Title being written.</param>
    /// <param name="outputDirectory">Output root.</param>
    /// <param name="format">Shape being written, which picks the prefix.</param>
    private static string TitleFolder(Game game, string outputDirectory, OutputFormat format)
    {
        var name = game.NameIn(Language.English) is { } localized
            ? Sanitize(localized.ShortName) ?? Sanitize(localized.LongName)
            : null;
        var stem = (format == OutputFormat.Loadiine ? LoadiinePrefix : TitlePrefix) + (name ?? game.TitleId.ToString());
        var folder = Path.Combine(outputDirectory, stem);
        for (var n = 2; Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder).Any(); n++)
            folder = Path.Combine(outputDirectory, $"{stem} ({n})");

        return folder;
    }

}
