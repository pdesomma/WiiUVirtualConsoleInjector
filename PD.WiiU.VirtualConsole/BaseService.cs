using PD.WiiU.VirtualConsole.Ports;

namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Default <see cref="IBaseService"/> over a catalog, a store, the user's keys and a downloader.
/// </summary>
public sealed class BaseService : IBaseService
{
    private readonly BaseCatalog _catalog;
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
    public BaseService(BaseCatalog catalog, IBaseStore store, IKeyStore keys, IBaseDownloader downloader)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _keys = keys ?? throw new ArgumentNullException(nameof(keys));
        _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
    }

    /// <inheritdoc/>
    public IReadOnlyList<BaseTitle> Available(SourceConsole console) => _catalog.For(console);

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
