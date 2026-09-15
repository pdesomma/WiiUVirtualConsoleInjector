using System.Collections.ObjectModel;
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
    [NotifyPropertyChangedFor(nameof(Arrows), nameof(HasArrows))]
    private PageViewModel _currentPage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLight))]
    private bool _isDark;

    /// <summary>
    /// Creates a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="inject">The inject page.</param>
    /// <param name="history">The history page.</param>
    /// <param name="bases">The bases and keys page.</param>
    /// <param name="settingsPage">The settings page.</param>
    /// <param name="credits">The acknowledgements page.</param>
    /// <param name="settings">Where the colour scheme is remembered.</param>
    /// <param name="toasts">The notices shown in the corner.</param>
    /// <param name="navigation">Requests from pages to show another page.</param>
    public MainWindowViewModel(InjectViewModel inject, HistoryViewModel history, BasesViewModel bases, SettingsViewModel settingsPage, AcknowledgementsViewModel credits, ISettingsService settings, IToastService toasts, INavigationService navigation)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        Toasts = (toasts ?? throw new ArgumentNullException(nameof(toasts))).Toasts;
        if (navigation is null)
            throw new ArgumentNullException(nameof(navigation));

        Pages = new PageViewModel[]
        {
            inject ?? throw new ArgumentNullException(nameof(inject)),
            history ?? throw new ArgumentNullException(nameof(history)),
            bases ?? throw new ArgumentNullException(nameof(bases)),
            settingsPage ?? throw new ArgumentNullException(nameof(settingsPage)),
            credits ?? throw new ArgumentNullException(nameof(credits)),
        };
        _currentPage = Pages[0];
        _isDark = settings.Current.Theme == AppTheme.Dark;
        navigation.Requested += (_, type) => CurrentPage = Pages.FirstOrDefault(p => p.GetType() == type) ?? CurrentPage;
    }

    /// <summary>
    /// The current page's side arrows, or null when it has none.
    /// </summary>
    public IArrowNavigation? Arrows => CurrentPage as IArrowNavigation;

    /// <summary>
    /// True when the current page is turned with the side arrows.
    /// </summary>
    public bool HasArrows => Arrows is not null;

    /// <summary>
    /// Notices shown in the corner of the window.
    /// </summary>
    public ReadOnlyObservableCollection<ToastViewModel> Toasts { get; }
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
