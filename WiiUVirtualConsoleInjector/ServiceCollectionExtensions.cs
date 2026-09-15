using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Infrastructure;
using PD.WiiU.VirtualConsole.Ports;
using WiiUVirtualConsoleInjector.Services;
using WiiUVirtualConsoleInjector.ViewModels;

namespace WiiUVirtualConsoleInjector;

/// <summary>
/// Registers everything the application runs on.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers domain services, infrastructure adapters, application services and view models.
    /// </summary>
    /// <param name="services">Collection to add to.</param>
    /// <param name="paths">Per-user folders.</param>
    /// <param name="owner">Resolves the window dialogs are modal to.</param>
    public static IServiceCollection AddApplication(this IServiceCollection services, AppPaths paths, Func<Window> owner)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));
        if (paths is null)
            throw new ArgumentNullException(nameof(paths));
        if (owner is null)
            throw new ArgumentNullException(nameof(owner));

        services.AddSingleton(paths);
        services.AddSingleton<ISettingsStore>(new JsonSettingsStore(paths.SettingsFile));
        services.AddSingleton<IUiScheduler, AvaloniaUiScheduler>();
        services.AddSingleton<IToastService, ToastService>();
        services.AddSingleton<IKeyStore>(p => new ToastingKeyStore(new JsonKeyStore(paths.KeysFile), p.GetRequiredService<IToastService>()));
        services.AddSingleton<IRemovableDrives, SystemRemovableDrives>();
        services.AddSingleton<IArtworkComposer>(p => new SkiaArtworkComposer(null, () => p.GetRequiredService<ISettingsService>().CaptionFontPath));
        services.AddSingleton<ISdCard, SdCard>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton(BaseCatalog.Bundled());
        services.AddSingleton(new HttpClient());
        services.AddSingleton<IBaseStore, CurrentBaseStore>();
        services.AddSingleton<IBaseDownloader>(p => new NusBaseDownloader(p.GetRequiredService<IBaseStore>(), p.GetRequiredService<HttpClient>(), paths.PackageCache));
        services.AddSingleton<IBaseService, BaseService>();
        services.AddSingleton<IInjectionServiceFactory, InjectionServiceFactory>();
        services.AddSingleton<IDialogService>(new AvaloniaDialogService(owner));
        services.AddSingleton<ILinkOpener>(new AvaloniaLinkOpener(owner));
        services.AddSingleton<INavigationService, NavigationService>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<InjectViewModel>();
        services.AddSingleton<BasesViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<AcknowledgementsViewModel>();
        return services;
    }
}
