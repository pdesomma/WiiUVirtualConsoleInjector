using PD.WiiU.VirtualConsole.Ports;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Default <see cref="ISdCard"/>: picks the likeliest removable drive and copies titles into its install folder.
/// </summary>
public sealed class SdCard : ISdCard
{
    /// <summary>
    /// Folder on the card that WUP Installer reads titles from.
    /// </summary>
    public const string InstallFolder = "install";

    private readonly IRemovableDrives _drives;

    /// <summary>
    /// Creates a new instance of the <see cref="SdCard"/> class.
    /// </summary>
    /// <param name="drives">Where the removable volumes come from.</param>
    public SdCard(IRemovableDrives drives)
    {
        _drives = drives ?? throw new ArgumentNullException(nameof(drives));
    }

    /// <inheritdoc/>
    public async Task<string> CopyAsync(string titleDirectory, string root, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(titleDirectory))
            throw new ArgumentException("Title directory is required.", nameof(titleDirectory));
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Card root is required.", nameof(root));
        if (!Directory.Exists(titleDirectory))
            throw new DirectoryNotFoundException($"{titleDirectory} is not there.");

        var files = Directory.GetFiles(titleDirectory, "*", SearchOption.AllDirectories);
        RequireRoom(files, root);

        var destination = Path.Combine(root, InstallFolder, Path.GetFileName(titleDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
        Directory.CreateDirectory(destination);
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var target = Path.Combine(destination, file.Substring(titleDirectory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            progress?.Report(Path.GetFileName(file));
            using var source = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            using var copy = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await source.CopyToAsync(copy, 81920, cancellationToken).ConfigureAwait(false);
        }
        return destination;
    }

    /// <inheritdoc/>
    public RemovableDrive? Detect()
    {
        var drives = Drives();
        var prepared = drives.Where(d => d.LooksPrepared).ToArray();

        return prepared.Length == 1 ? prepared[0] : prepared.Length == 0 && drives.Count == 1 ? drives[0] : null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<RemovableDrive> Drives() => _drives.List();

    /// <summary>
    /// Throws when the card has less room than the files need.
    /// </summary>
    /// <param name="files">Files to be copied.</param>
    /// <param name="root">Card root.</param>
    private static void RequireRoom(IEnumerable<string> files, string root)
    {
        var needed = files.Sum(f => new FileInfo(f).Length);
        var free = new DriveInfo(root).AvailableFreeSpace;
        if (needed > free)
            throw new IOException($"The card has {free / 1048576} MB free; the title needs {needed / 1048576} MB.");
    }
}
