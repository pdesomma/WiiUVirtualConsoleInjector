using PD.WiiU.VirtualConsole;

namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// Community compatibility tables, one page per console.
/// </summary>
public interface ICompatibilityLists
{
    /// <summary>
    /// Page that lists tested games, bases and settings for a console.
    /// </summary>
    /// <param name="console">Console the list is for.</param>
    Uri For(SourceConsole console);

    /// <summary>
    /// Opens the console's page in the browser.
    /// </summary>
    /// <param name="console">Console the list is for.</param>
    /// <returns>False when nothing could open it.</returns>
    Task<bool> OpenAsync(SourceConsole console);
}
