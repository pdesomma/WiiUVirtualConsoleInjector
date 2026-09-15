using CommunityToolkit.Mvvm.ComponentModel;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// The shell: a list of pages and the one on screen.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private PageViewModel _currentPage;

    /// <summary>
    /// Creates a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="inject">The inject page.</param>
    /// <param name="bases">The bases and keys page.</param>
    /// <param name="settings">The settings page.</param>
    public MainWindowViewModel(InjectViewModel inject, BasesViewModel bases, SettingsViewModel settings)
    {
        Pages = new PageViewModel[]
        {
            inject ?? throw new ArgumentNullException(nameof(inject)),
            bases ?? throw new ArgumentNullException(nameof(bases)),
            settings ?? throw new ArgumentNullException(nameof(settings)),
        };
        _currentPage = Pages[0];
    }

    /// <summary>
    /// Pages in navigation order.
    /// </summary>
    public IReadOnlyList<PageViewModel> Pages { get; }

    /// <summary>
    /// Window title.
    /// </summary>
    public string Title => AppPaths.FolderName;

    partial void OnCurrentPageChanged(PageViewModel value) => _ = value.ActivateAsync();
}
