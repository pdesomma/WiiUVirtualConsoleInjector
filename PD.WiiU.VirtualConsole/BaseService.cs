using PD.WiiU.VirtualConsole.Ports;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Default <see cref="IBaseService"/> over a catalog, a store, the user's keys and a downloader.
/// </summary>
public sealed class BaseService : IBaseService
{
    private readonly BaseCatalog _catalog;
    private readonly ICustomBases _custom;
    private readonly IBaseDownloader _downloader;
    private readonly IKeyStore _keys;
    private readonly IBaseStore _store;

    /// <summary>
    /// Creates a new instance of the <see cref="BaseService"/> class.
    /// </summary>
    /// <param name="catalog">Known bases.</param>
    /// <param name="store">Where bases live.</param>
    /// <param name="keys">The user's keys.</param>
    /// <param name="downloader">Fetches bases into the store.</param>
    /// <param name="custom">Bases the user added.</param>
    public BaseService(BaseCatalog catalog, IBaseStore store, IKeyStore keys, IBaseDownloader downloader, ICustomBases custom)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
        _custom = custom ?? throw new ArgumentNullException(nameof(custom));
    }

    /// <inheritdoc/>
    public void AddCustom(BaseTitle @base)
    {
        if (@base is null)
            throw new ArgumentNullException(nameof(@base));

        _custom.Add(@base);
    }

    /// <inheritdoc/>
    public IReadOnlyList<BaseTitle> Available(SourceConsole console)
    {
        // a custom Wii base serves GameCube and a custom GBA base serves Game Boy, as the catalog's do
        var custom = _custom.All().Where(t => t.Console == console).ToList();
        var host = BaseCatalog.HostConsole(console);
        if (host != console)
            custom.AddRange(_custom.All().Where(t => t.Console == host).Select(t => new BaseTitle(t.TitleId, t.Name, t.Region, console) { IsCustom = true }));
        var catalog = _catalog.For(console);
        return catalog.Concat(custom.Where(c => catalog.All(k => k.TitleId != c.TitleId))).ToList();
    }

    /// <inheritdoc/>
    public Task<TitleDirectory> ImportAsync(BaseTitle @base, string sourceDirectory, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        if (@base is null)
            throw new ArgumentNullException(nameof(@base));
        if (string.IsNullOrWhiteSpace(sourceDirectory))
            throw new ArgumentException("Source folder is required.", nameof(sourceDirectory));

        return _store.ImportAsync(@base, sourceDirectory, _keys.CommonKey, progress, cancellationToken);
    }

    /// <inheritdoc/>
    public bool RemoveCustom(WiiUSharp.TitleId titleId) => _custom.Remove(titleId);

    /// <inheritdoc/>
    public Task<TitleDirectory> DownloadAsync(BaseTitle @base, IProgress<BaseDownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        if (@base is null)
            throw new ArgumentNullException(nameof(@base));

        var commonKey = _keys.CommonKey ?? throw new InvalidOperationException("The common key has not been supplied.");
        var titleKey = _keys.GetTitleKey(@base.TitleId) ?? throw new InvalidOperationException($"No title key has been supplied for {@base}.");
        return _downloader.DownloadAsync(@base, titleKey, commonKey, progress, cancellationToken);
    }

    /// <inheritdoc/>
    /// <inheritdoc/>
    public bool HasTitleKey(BaseTitle @base)
    {
        if (@base is null)
            throw new ArgumentNullException(nameof(@base));

        return _keys.GetTitleKey(@base.TitleId) is not null;
    }

    /// <inheritdoc/>
    public ByteSize? SizeOnDisk(BaseTitle @base)
    {
        if (@base is null)
            throw new ArgumentNullException(nameof(@base));

        var title = _store.Locate(@base);
        return title.Exists ? ByteSize.OfDirectory(title.Root) : null;
    }

    /// <inheritdoc/>
    public BaseStatus Status(BaseTitle @base)
    {
        if (@base is null)
            throw new ArgumentNullException(nameof(@base));

        if (_store.Locate(@base).Exists)
            return BaseStatus.Present;
        if (_keys.CommonKey is null)
            return BaseStatus.NeedsCommonKey;
        if (_keys.GetTitleKey(@base.TitleId) is null)
            return BaseStatus.NeedsTitleKey;
        return BaseStatus.Downloadable;
    }
}
