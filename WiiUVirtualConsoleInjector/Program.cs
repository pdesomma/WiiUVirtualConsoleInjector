using Avalonia;

namespace WiiUVirtualConsoleInjector;

/// <summary>
/// Process entry point; nothing Avalonia-dependent may run before the app builder starts.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Avalonia configuration; also used by the designer.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();

    /// <summary>
    /// Starts the desktop lifetime.
    /// </summary>
    /// <param name="args">Command line.</param>
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
}
