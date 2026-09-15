using System.Net.Http;
using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp.Nus;

namespace PD.WiiU.VirtualConsole.Infrastructure;

/// <summary>
/// Fetches a base's package via WiiUSharp.Nus, then unpacks it into the store.
/// </summary>
public sealed class NusBaseDownloader : IBaseDownloader
{
    /// <summary>
    /// Suffix of the sibling folder a base is unpacked into before it is moved into place.
    /// </summary>
    public const string TempSuffix = ".tmp";

    private readonly NusDownloader _downloader;
    private readonly IBaseStore _store;

    /// <summary>
    /// Creates a new instance of the <see cref="NusBaseDownloader"/> class.
    /// </summary>
    /// <param name="store">Where the unpacked base ends up.</param>
    /// <param name="client">Client to fetch with; the caller owns it.</param>
    /// <param name="packageDirectory">Folder caching downloaded packages, one subfolder per title ID.</param>
    /// <param name="baseUrl">Server root ending in a slash, or null for the public content server.</param>
    public NusBaseDownloader(IBaseStore store, HttpClient client, string packageDirectory, string? baseUrl = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _downloader = new NusDownloader(client, baseUrl);
        if (string.IsNullOrWhiteSpace(packageDirectory))
            throw new ArgumentException("Package directory is required.", nameof(packageDirectory));

        PackageDirectory = Path.GetFullPath(packageDirectory);
    }

    /// <summary>
    /// Server root the title folders hang off.
    /// </summary>
    public string BaseUrl => _downloader.BaseUrl;

    /// <summary>
    /// Folder caching downloaded packages, one subfolder per title ID.
    /// </summary>
    public string PackageDirectory { get; }

    /// <inheritdoc/>
    public async Task<TitleDirectory> DownloadAsync(BaseTitle @base, EncryptedTitleKey titleKey, CommonKey commonKey, IProgress<BaseDownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (@base is null)
            throw new ArgumentNullException(nameof(@base));

        var target = _store.Locate(@base);
        var package = Path.Combine(PackageDirectory, @base.TitleId.ToString());
        await _downloader.DownloadAsync(@base.TitleId, package, titleKey, commonKey, DownloadProgress(progress), cancellationToken).ConfigureAwait(false);
        await Task.Run(() => Unpack(package, target, commonKey, progress, cancellationToken), cancellationToken).ConfigureAwait(false);
        Directory.Delete(package, recursive: true);
        return target;
    }

    private static void DeleteIfExists(string directory)
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    private static IProgress<NusDownloadProgress>? DownloadProgress(IProgress<BaseDownloadProgress>? progress) =>
        progress is null
            ? null
            : new Relay<NusDownloadProgress>(p => progress.Report(new BaseDownloadProgress(BaseDownloadPhase.Downloading, p.File, p.FileNumber, p.FileCount, p.BytesReceived, p.BytesTotal)));

    private static void Unpack(string package, TitleDirectory target, CommonKey commonKey, IProgress<BaseDownloadProgress>? progress, CancellationToken cancellationToken)
    {
        var temp = target.Root + TempSuffix;
        DeleteIfExists(temp);
        try
        {
            new NusUnpacker(commonKey).Unpack(package, temp, UnpackProgress(progress), cancellationToken);
        }
        catch
        {
            DeleteIfExists(temp);
            throw;
        }

        DeleteIfExists(target.Root);
        Directory.Move(temp, target.Root);
    }

    private static IProgress<string>? UnpackProgress(IProgress<BaseDownloadProgress>? progress)
    {
        if (progress is null)
            return null;

        var number = 0;
        return new Relay<string>(message => progress.Report(new BaseDownloadProgress(BaseDownloadPhase.Unpacking, message, ++number, 0, 0, null)));
    }

    /// <summary>
    /// Forwards reports synchronously so ordering matches the source.
    /// </summary>
    private sealed class Relay<T> : IProgress<T>
    {
        private readonly Action<T> _report;

        /// <summary>
        /// Creates a new instance of the <see cref="Relay{T}"/> class.
        /// </summary>
        /// <param name="report">Receives each value.</param>
        public Relay(Action<T> report)
        {
            _report = report;
        }

        /// <inheritdoc/>
        public void Report(T value) => _report(value);
    }
}
