using PD.WiiU.VirtualConsole;

namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// Builds an <see cref="IInjectionService"/> around the keys and paths in force right now.
/// </summary>
public interface IInjectionServiceFactory
{
    /// <summary>
    /// A service over the current base store, packing with the user's common key.
    /// </summary>
    /// <exception cref="InvalidOperationException">The Wii U common key is missing.</exception>
    IInjectionService Create();

    /// <summary>
    /// Names of keys an inject for the console still needs; empty when it can run.
    /// </summary>
    /// <param name="console">Console the ROM is for.</param>
    /// <param name="romPath">The ROM, when picked; a Wii image only needs the Wii common key when it is encrypted.</param>
    IReadOnlyList<string> MissingKeys(SourceConsole console, string? romPath = null);
}
