using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// The four artwork slots, each built entirely on its own.
/// </summary>
public sealed class ArtworkBuilderViewModel : ViewModelBase
{
    /// <summary>
    /// Creates a new instance of the <see cref="ArtworkBuilderViewModel"/> class.
    /// </summary>
    /// <param name="composer">Draws the images.</param>
    /// <param name="dialogs">Image pickers and errors.</param>
    /// <param name="workFolder">Folder built images are written under.</param>
    public ArtworkBuilderViewModel(IArtworkComposer composer, IDialogService dialogs, Func<string> workFolder)
    {
        if (composer is null)
            throw new ArgumentNullException(nameof(composer));
        if (dialogs is null)
            throw new ArgumentNullException(nameof(dialogs));
        if (workFolder is null)
            throw new ArgumentNullException(nameof(workFolder));

        Icon = new ArtworkSlotViewModel(ImageSlot.Icon, composer, dialogs, workFolder);
        Tv = new ArtworkSlotViewModel(ImageSlot.BootTv, composer, dialogs, workFolder);
        GamePad = new ArtworkSlotViewModel(ImageSlot.BootDrc, composer, dialogs, workFolder);
        Logo = new ArtworkSlotViewModel(ImageSlot.BootLogo, composer, dialogs, workFolder);
        Slots = new[] { Icon, Tv, GamePad, Logo };
        foreach (var slot in Slots)
            slot.Built += (_, e) => Built?.Invoke(this, e);
    }

    /// <summary>
    /// Raised with the slot and the PNG it was built into.
    /// </summary>
    public event EventHandler<ArtworkBuiltEventArgs>? Built;

    /// <summary>
    /// The GamePad boot screen.
    /// </summary>
    public ArtworkSlotViewModel GamePad { get; }
    /// <summary>
    /// The menu icon.
    /// </summary>
    public ArtworkSlotViewModel Icon { get; }
    /// <summary>
    /// The boot logo.
    /// </summary>
    public ArtworkSlotViewModel Logo { get; }
    /// <summary>
    /// All four, in the order they are shown.
    /// </summary>
    public IReadOnlyList<ArtworkSlotViewModel> Slots { get; }
    /// <summary>
    /// The TV boot screen.
    /// </summary>
    public ArtworkSlotViewModel Tv { get; }

    /// <summary>
    /// Forgets every slot's source image and captions.
    /// </summary>
    public void Clear()
    {
        foreach (var slot in Slots)
            slot.Clear();
    }

    /// <summary>
    /// Offers each slot the console's overlays and fills in blank captions from the wizard's names.
    /// </summary>
    /// <param name="console">Console being injected.</param>
    /// <param name="longName">Long name, comma-separated lines.</param>
    /// <param name="shortName">Short name, or null.</param>
    public void Refresh(SourceConsole console, string? longName, string? shortName = null)
    {
        foreach (var slot in Slots)
        {
            slot.Refresh(console);
            slot.Seed(longName, shortName);
        }
    }
}
