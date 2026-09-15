using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PD.WiiU.VirtualConsole;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// The shell: header, navigation rail, the page on screen and the colour scheme.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;

    [ObservableProperty]
    private PageViewModel _currentPage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLight))]
    private bool _isDark;

    /// <summary>
    /// Creates a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="inject">The inject page.</param>
    /// <param name="bases">The bases and keys page.</param>
    /// <param name="settingsPage">The settings page.</param>
    /// <param name="settings">Where the colour scheme is remembered.</param>
    /// <param name="navigation">Requests from pages to show another page.</param>
    public MainWindowViewModel(InjectViewModel inject, BasesViewModel bases, SettingsViewModel settingsPage, ISettingsService settings, INavigationService navigation)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        if (navigation is null)
            throw new ArgumentNullException(nameof(navigation));

        Pages = new PageViewModel[]
        {
            inject ?? throw new ArgumentNullException(nameof(inject)),
            bases ?? throw new ArgumentNullException(nameof(bases)),
            settingsPage ?? throw new ArgumentNullException(nameof(settingsPage)),
        };
        _currentPage = Pages[0];
        _isDark = settings.Current.Theme == AppTheme.Dark;
        navigation.Requested += (_, type) => CurrentPage = Pages.FirstOrDefault(p => p.GetType() == type) ?? CurrentPage;
    }

    /// <summary>
    /// True while the light scheme is on.
    /// </summary>
    public bool IsLight => !IsDark;

    /// <summary>
    /// Pages in navigation order.
    /// </summary>
    public IReadOnlyList<PageViewModel> Pages { get; }

    /// <summary>
    /// Window title.
    /// </summary>
    public string Title => "Virtual Console Injector";

    partial void OnCurrentPageChanged(PageViewModel value) => _ = value.ActivateAsync();

    partial void OnIsDarkChanged(bool value)
    {
        var theme = value ? AppTheme.Dark : AppTheme.Light;
        if (_settings.Current.Theme != theme)
            _settings.Update(s => s with { Theme = theme });
    }

    /// <summary>
    /// Switches to the dark scheme.
    /// </summary>
    [RelayCommand]
    private void UseDark() => IsDark = true;

    /// <summary>
    /// Switches to the light scheme.
    /// </summary>
    [RelayCommand]
    private void UseLight() => IsDark = false;
}
