using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using WiiUVirtualConsoleInjector.ViewModels;
using WiiUVirtualConsoleInjector.Views;

namespace WiiUVirtualConsoleInjector;

/// <summary>
/// Builds the service container and opens the main window.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Services for the running application; null until the framework has initialized.
    /// </summary>
    public IServiceProvider? Services { get; private set; }

    /// <inheritdoc/>
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            Services = new ServiceCollection().AddApplication(AppPaths.Default(), () => window).BuildServiceProvider();
            window.DataContext = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
