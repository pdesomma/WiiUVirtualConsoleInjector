using PD.WiiU.VirtualConsole.Ports;

namespace PD.WiiU.VirtualConsole.Infrastructure;

/// <summary>
/// Bases kept as unpacked folders named by title ID under one root.
/// </summary>
public sealed class DirectoryBaseStore : IBaseStore
{
    private const int CopyBufferSize = 1 << 20;

    /// <summary>
    /// Creates a new instance of the <see cref="DirectoryBaseStore"/> class.
    /// </summary>
    /// <param name="root">Folder holding one subfolder per base.</param>
    public DirectoryBaseStore(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));

        Root = Path.GetFullPath(root);
    }

    /// <summary>
    /// Folder holding one subfolder per base.
    /// </summary>
    public string Root { get; }

    /// <summary>
    /// Folder a base lives in.
    /// </summary>
    /// <param name="base">Base to locate.</param>
    public TitleDirectory Locate(BaseTitle @base)
    {
        if (@base is null)
            throw new ArgumentNullException(nameof(@base));

        return new TitleDirectory(Path.Combine(Root, @base.TitleId.ToString()));
    }

    /// <inheritdoc/>
    /// <exception cref="DirectoryNotFoundException">Base folder is missing or incomplete.</exception>
    public async Task<TitleDirectory> StageAsync(BaseTitle @base, string destination, CancellationToken cancellationToken = default)
    {
        var source = Locate(@base);
        if (!source.Exists)
            throw new DirectoryNotFoundException($"Base {@base} is not at {source}.");

        var target = TitleDirectory.Create(destination);
        await CopyTree(source.Code, target.Code, cancellationToken).ConfigureAwait(false);
        await CopyTree(source.Content, target.Content, cancellationToken).ConfigureAwait(false);
        await CopyTree(source.Meta, target.Meta, cancellationToken).ConfigureAwait(false);
        return target;
    }

    private static async Task CopyTree(string source, string destination, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.GetDirectories(source))
            await CopyTree(directory, Path.Combine(destination, Path.GetFileName(directory)), cancellationToken).ConfigureAwait(false);

        foreach (var file in Directory.GetFiles(source))
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, CopyBufferSize, useAsync: true);
            using var output = new FileStream(Path.Combine(destination, Path.GetFileName(file)), FileMode.Create, FileAccess.Write, FileShare.None, CopyBufferSize, useAsync: true);
            await input.CopyToAsync(output, CopyBufferSize, cancellationToken).ConfigureAwait(false);
        }
    }
}
