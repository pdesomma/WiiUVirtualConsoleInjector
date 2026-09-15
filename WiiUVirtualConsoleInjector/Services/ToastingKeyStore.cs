using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp;
using WiiUSharp.Nus;

namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// Wraps a key store and toasts after every write.
/// </summary>
public sealed class ToastingKeyStore : IKeyStore
{
    /// <summary>
    /// What every write toasts.
    /// </summary>
    public const string SavedText = "Saved";

    private readonly IKeyStore _inner;
    private readonly IToastService _toasts;

    /// <summary>
    /// Creates a new instance of the <see cref="ToastingKeyStore"/> class.
    /// </summary>
    /// <param name="inner">Store that does the work.</param>
    /// <param name="toasts">Where the notice goes.</param>
    public ToastingKeyStore(IKeyStore inner, IToastService toasts)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _toasts = toasts ?? throw new ArgumentNullException(nameof(toasts));
    }

    /// <inheritdoc/>
    public AncastKey? AncastKey
    {
        get => _inner.AncastKey;
        set
        {
            _inner.AncastKey = value;
            _toasts.Show(SavedText);
        }
    }
    /// <inheritdoc/>
    public CommonKey? CommonKey
    {
        get => _inner.CommonKey;
        set
        {
            _inner.CommonKey = value;
            _toasts.Show(SavedText);
        }
    }
    /// <inheritdoc/>
    public WiiSharp.CommonKey? WiiCommonKey
    {
        get => _inner.WiiCommonKey;
        set
        {
            _inner.WiiCommonKey = value;
            _toasts.Show(SavedText);
        }
    }

    /// <inheritdoc/>
    public EncryptedTitleKey? GetTitleKey(TitleId titleId) => _inner.GetTitleKey(titleId);

    /// <inheritdoc/>
    public void SetTitleKey(TitleId titleId, EncryptedTitleKey? titleKey)
    {
        _inner.SetTitleKey(titleId, titleKey);
        _toasts.Show(SavedText);
    }
}
