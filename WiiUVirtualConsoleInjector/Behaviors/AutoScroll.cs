using Avalonia;
using Avalonia.Controls;

namespace WiiUVirtualConsoleInjector.Behaviors;

/// <summary>
/// Keeps a scroll viewer pinned to its last line as content is appended, unless the user has scrolled up.
/// </summary>
public static class AutoScroll
{
    /// <summary>
    /// Slack in pixels still counted as sitting at the bottom.
    /// </summary>
    public const double Slack = 2;

    /// <summary>
    /// Set true on a scroll viewer to have it follow new content.
    /// </summary>
    public static readonly AttachedProperty<bool> ToEndProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewer, bool>("ToEnd", typeof(AutoScroll));

    static AutoScroll()
    {
        ToEndProperty.Changed.AddClassHandler<ScrollViewer, bool>((viewer, e) => Attach(viewer, e.NewValue.Value));
    }

    /// <summary>
    /// Reads the flag.
    /// </summary>
    /// <param name="viewer">Viewer to read.</param>
    public static bool GetToEnd(ScrollViewer viewer) => (viewer ?? throw new ArgumentNullException(nameof(viewer))).GetValue(ToEndProperty);

    /// <summary>
    /// Sets the flag.
    /// </summary>
    /// <param name="viewer">Viewer to change.</param>
    /// <param name="value">True to follow new content.</param>
    public static void SetToEnd(ScrollViewer viewer, bool value) => (viewer ?? throw new ArgumentNullException(nameof(viewer))).SetValue(ToEndProperty, value);

    /// <summary>
    /// True when the view sat at the bottom before the content grew, so it should follow.
    /// </summary>
    /// <param name="offset">Vertical offset before the change.</param>
    /// <param name="extent">Content height before the change.</param>
    /// <param name="viewport">Visible height before the change.</param>
    public static bool ShouldFollow(double offset, double extent, double viewport) => offset >= extent - viewport - Slack;

    /// <summary>
    /// Starts or stops following on one viewer.
    /// </summary>
    /// <param name="viewer">Viewer to wire up.</param>
    /// <param name="follow">True to follow new content.</param>
    private static void Attach(ScrollViewer viewer, bool follow)
    {
        viewer.ScrollChanged -= OnScrollChanged;
        if (follow)
            viewer.ScrollChanged += OnScrollChanged;
    }

    /// <summary>
    /// Scrolls to the end when content was appended and the view was already at the bottom.
    /// </summary>
    /// <param name="sender">Viewer that scrolled.</param>
    /// <param name="e">What changed.</param>
    private static void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer viewer || e.ExtentDelta.Y <= 0)
            return;

        if (ShouldFollow(viewer.Offset.Y - e.OffsetDelta.Y, viewer.Extent.Height - e.ExtentDelta.Y, viewer.Viewport.Height - e.ViewportDelta.Y))
            viewer.ScrollToEnd();
    }
}
