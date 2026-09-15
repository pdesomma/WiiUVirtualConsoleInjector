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
    /// <param name="icon">Asset path of the navigation glyph, relative to the Assets folder.</param>
    /// <param name="glyph">SVG path data drawn instead of the asset when given; stroked, 24-unit box.</param>
    protected PageViewModel(string title, string icon, string? glyph = null)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        NavIcon = icon ?? throw new ArgumentNullException(nameof(icon));
        NavGlyph = glyph;
    }

    /// <summary>
    /// True when the rail draws a stroked path rather than the asset.
    /// </summary>
    public bool HasNavGlyph => NavGlyph is not null;

    /// <summary>
    /// SVG path data drawn in the rail instead of the asset, or null.
    /// </summary>
    public string? NavGlyph { get; }

    /// <summary>
    /// Asset path of the navigation glyph, relative to the Assets folder; white, for the dark rail.
    /// </summary>
    public string NavIcon { get; }
    /// <summary>
    /// The same glyph in near-black, for the light rail.
    /// </summary>
    public string NavIconDark => "Dark/" + NavIcon;
    /// <summary>
    /// Label in the navigation list.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Runs each time the page is shown; refresh anything that may have changed elsewhere.
    /// </summary>
    public virtual Task ActivateAsync() => Task.CompletedTask;
}
