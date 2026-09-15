using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
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
            var shell = Services.GetRequiredService<MainWindowViewModel>();
            ApplyTheme(shell.IsDark);
            shell.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainWindowViewModel.IsDark))
                    ApplyTheme(shell.IsDark);
            };
            window.DataContext = shell;
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ApplyTheme(bool dark) => RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
}
