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
    public BaseChoice(BaseTitle @base, BaseStatus status)
    {
        Base = @base ?? throw new ArgumentNullException(nameof(@base));
        Status = status;
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
    public bool IsPresent => Status == BaseStatus.Present;

    /// <summary>
    /// Status in the store when the list was built.
    /// </summary>
    public BaseStatus Status { get; }

    /// <inheritdoc/>
    public override string ToString() => Display;
}
