namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// A page reachable from the side navigation.
/// </summary>
public abstract class PageViewModel : ViewModelBase
{
    /// <summary>
    /// Creates a new instance of the <see cref="PageViewModel"/> class.
    /// </summary>
    /// <param name="title">Label in the navigation list.</param>
    protected PageViewModel(string title)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
    }

    /// <summary>
    /// Label in the navigation list.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Runs each time the page is shown; refresh anything that may have changed elsewhere.
    /// </summary>
    public virtual Task ActivateAsync() => Task.CompletedTask;
}
