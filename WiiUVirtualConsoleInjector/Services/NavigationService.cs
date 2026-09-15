using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// Default <see cref="INavigationService"/>: an event the shell listens to.
/// </summary>
public sealed class NavigationService : INavigationService
{
    /// <inheritdoc/>
    public event EventHandler<Type>? Requested;

    /// <inheritdoc/>
    public void Show<TPage>() where TPage : PageViewModel => Requested?.Invoke(this, typeof(TPage));
}
