using Avalonia.Controls;

namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// Opens links through the owning window's launcher.
/// </summary>
public sealed class AvaloniaLinkOpener : ILinkOpener
{
    private readonly Func<Window> _owner;

    /// <summary>
    /// Creates a new instance of the <see cref="AvaloniaLinkOpener"/> class.
    /// </summary>
    /// <param name="owner">Resolves the window whose launcher is used.</param>
    public AvaloniaLinkOpener(Func<Window> owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    /// <inheritdoc/>
    public Task<bool> OpenAsync(Uri uri)
    {
        if (uri is null)
            throw new ArgumentNullException(nameof(uri));

        return _owner().Launcher.LaunchUriAsync(uri);
    }
}
