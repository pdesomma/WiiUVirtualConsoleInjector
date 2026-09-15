using CommunityToolkit.Mvvm.Input;
using WiiUVirtualConsoleInjector.Assets;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// The acknowledgements page: what is shipped and what was learned from.
/// </summary>
public sealed partial class AcknowledgementsViewModel : PageViewModel
{
    private const string HeartGlyph = "M12 20.5 4.6 13.2a4.3 4.3 0 0 1 6.1-6.1L12 8.4l1.3-1.3a4.3 4.3 0 0 1 6.1 6.1Z";

    private readonly ILinkOpener _links;

    /// <summary>
    /// Creates a new instance of the <see cref="AcknowledgementsViewModel"/> class.
    /// </summary>
    /// <param name="links">Opens project pages.</param>
    public AcknowledgementsViewModel(ILinkOpener links)
        : base("Credits", "help.png", HeartGlyph)
    {
        _links = links ?? throw new ArgumentNullException(nameof(links));
    }

    /// <summary>
    /// Projects whose logic was reimplemented here.
    /// </summary>
    public IReadOnlyList<Acknowledgement> Borrowed => Acknowledgements.Borrowed;
    /// <summary>
    /// Binaries and fonts shipped unmodified.
    /// </summary>
    public IReadOnlyList<Acknowledgement> Shipped => Acknowledgements.Shipped;

    /// <summary>
    /// Opens the entry's project page, when it has one.
    /// </summary>
    /// <param name="entry">Entry clicked.</param>
    [RelayCommand]
    private Task OpenAsync(Acknowledgement? entry) =>
        entry?.Url is { } url ? _links.OpenAsync(url) : Task.CompletedTask;
}
