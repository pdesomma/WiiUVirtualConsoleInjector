using System.Windows.Input;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// A page turned with the big side arrows, which the window pins to the middle of the view rather than the page.
/// </summary>
public interface IArrowNavigation
{
    /// <summary>
    /// Turns forward.
    /// </summary>
    ICommand NextCommand { get; }
    /// <summary>
    /// Tooltip for the forward arrow.
    /// </summary>
    string NextHint { get; }
    /// <summary>
    /// Turns back.
    /// </summary>
    ICommand PreviousCommand { get; }
    /// <summary>
    /// Tooltip for the back arrow.
    /// </summary>
    string PreviousHint { get; }
}
