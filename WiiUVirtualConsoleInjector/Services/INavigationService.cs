namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// Lets a page ask the shell to show another page.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Raised with the page type to show.
    /// </summary>
    event EventHandler<Type>? Requested;

    /// <summary>
    /// Asks the shell to show the page of a type.
    /// </summary>
    /// <typeparam name="TPage">Page view model type.</typeparam>
    void Show<TPage>() where TPage : ViewModels.PageViewModel;
}
