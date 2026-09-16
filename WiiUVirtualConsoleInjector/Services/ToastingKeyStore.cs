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
    /// Title of a toast for a key that was forgotten.
    /// </summary>
    public const string ClearedText = "Cleared";
    /// <summary>
    /// Title of a toast for a key that was stored.
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
    public CommonKey? CommonKey
    {
        get => _inner.CommonKey;
        set
        {
            _inner.CommonKey = value;
            Toast(value is not null, "Wii U common key");
        }
    }
    /// <inheritdoc/>
    public WiiSharp.CommonKey? WiiCommonKey
    {
        get => _inner.WiiCommonKey;
        set
        {
            _inner.WiiCommonKey = value;
            Toast(value is not null, "Wii common key");
        }
    }

    /// <inheritdoc/>
    public EncryptedTitleKey? GetTitleKey(TitleId titleId) => _inner.GetTitleKey(titleId);

    /// <inheritdoc/>
    public void SetTitleKey(TitleId titleId, EncryptedTitleKey? titleKey)
    {
        _inner.SetTitleKey(titleId, titleKey);
        Toast(titleKey is not null, $"Title key for {titleId}");
    }

    /// <summary>
    /// Reports what was stored or forgotten.
    /// </summary>
    /// <param name="stored">True when a key was written, false when it was removed.</param>
    /// <param name="what">Name of the key.</param>
    private void Toast(bool stored, string what) =>
        _toasts.Show(ToastKind.Success, stored ? SavedText : ClearedText, $"{what} {(stored ? "stored" : "forgotten")}.");
}
