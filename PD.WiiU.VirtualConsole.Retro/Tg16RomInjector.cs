using PD.WiiU.VirtualConsole.Ports;

namespace PD.WiiU.VirtualConsole.Retro;

/// <summary>
/// Injects a TurboGrafx-16 HuCard ROM, or a TurboCD folder, into a Virtual Console base as content/pceemu/pce.pkg.
/// </summary>
public sealed class Tg16RomInjector : IRomInjector
{
    /// <summary>
    /// Folder under content holding the package.
    /// </summary>
    public const string EmulatorFolder = "pceemu";

    /// <inheritdoc/>
    public SourceConsole Console => SourceConsole.Tg16;

    /// <inheritdoc/>
    public IReadOnlyList<BaseIssue> Inspect(TitleDirectory title)
    {
        if (title is null)
            throw new ArgumentNullException(nameof(title));

        var inspection = new BaseInspection(title);
        inspection.RequireFile(TitleDirectory.ContentFolder + "/" + EmulatorFolder + "/" + PcePackage.FileName, 1);
        return inspection.Issues;
    }

    /// <inheritdoc/>
    /// <remarks>A ROM path that is a folder is packed as a TurboCD game.</remarks>
    public Task InjectAsync(Injection injection, TitleDirectory title, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (injection is null)
            throw new ArgumentNullException(nameof(injection));
        if (title is null)
            throw new ArgumentNullException(nameof(title));
        if (injection.Console != SourceConsole.Tg16)
            throw new ArgumentException($"Injection is for {injection.Console}.", nameof(injection));

        var source = injection.Rom.Path;
        byte[] package;
        if (Directory.Exists(source))
        {
            progress?.Report("Packing TurboCD folder " + Path.GetFileName(source.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
            package = PcePackage.BuildTurboCd(source);
        }
        else
        {
            progress?.Report("Packing " + Path.GetFileName(source));
            package = PcePackage.BuildHuCard(Path.GetFileName(source), File.ReadAllBytes(source));
        }
        cancellationToken.ThrowIfCancellationRequested();

        var folder = Path.Combine(title.Content, EmulatorFolder);
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, PcePackage.FileName), package);
        return Task.CompletedTask;
    }
}
