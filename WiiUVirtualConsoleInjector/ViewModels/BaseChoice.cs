using PD.WiiU.VirtualConsole;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// A base in the inject page's list with the status it had when the list was built.
/// </summary>
public sealed class BaseChoice
{
    /// <summary>
    /// Creates a new instance of the <see cref="BaseChoice"/> class.
    /// </summary>
    /// <param name="base">The base.</param>
    /// <param name="status">Its status in the store.</param>
    public BaseChoice(BaseTitle @base, BaseStatus status, bool keysOk = true, bool hasTitleKey = false)
    {
        Base = @base ?? throw new ArgumentNullException(nameof(@base));
        Status = status;
        KeysOk = keysOk;
        HasTitleKey = hasTitleKey;
    }

    /// <summary>
    /// The base.
    /// </summary>
    public BaseTitle Base { get; }

    /// <summary>
    /// List text: name, region and a note when it is not in the store.
    /// </summary>
    public string Display => IsPresent ? Base.ToString() : $"{Base} (not downloaded)";

    /// <summary>
    /// True when the base can be injected into.
    /// </summary>
    /// <summary>
    /// True when this base's own title key has been added.
    /// </summary>
    public bool HasTitleKey { get; }
    public bool IsPresent => Status == BaseStatus.Present;

    /// <summary>
    /// True when the base can be picked for an inject: downloaded and unlocked.
    /// </summary>
    public bool IsUsable => IsPresent && KeysOk;

    /// <summary>
    /// True when the user has every key an inject into this base needs.
    /// </summary>
    public bool KeysOk { get; }

    /// <summary>
    /// Region name for the flag swatch.
    /// </summary>
    public string Region => Base.Region.ToString();

    /// <summary>
    /// Status in the store when the list was built.
    /// </summary>
    public BaseStatus Status { get; }

    /// <summary>
    /// Title ID as sixteen hex digits.
    /// </summary>
    public string TitleId => Base.TitleId.ToString();

    /// <inheritdoc/>
    public override string ToString() => Display;
}
