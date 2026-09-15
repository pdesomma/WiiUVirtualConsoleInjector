using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// Builds one artwork slot at a time from a screenshot: shared captions, a frame per slot, and a build per slot.
/// </summary>
public sealed partial class ArtworkBuilderViewModel : ViewModelBase
{
    private static readonly FileFilter[] ImageFilters = { new("Images", "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.webp", "*.tga"), new("All files", "*") };

    private readonly IArtworkComposer _composer;
    private readonly IDialogService _dialogs;
    private readonly Func<string> _workFolder;

    [ObservableProperty]
    private ArtworkFrame? _gamePadFrame;
    [ObservableProperty]
    private ArtworkFrame? _iconFrame;
    [ObservableProperty]
    private bool _isBuilding;
    [ObservableProperty]
    private ArtworkFrame? _logoFrame;
    [ObservableProperty]
    private string? _logoText;
    [ObservableProperty]
    private string? _nameLine1;
    [ObservableProperty]
    private string? _nameLine2;
    [ObservableProperty]
    private string? _players;
    [ObservableProperty]
    private string? _releaseYear;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasScreenshot))]
    [NotifyCanExecuteChangedFor(nameof(BuildCommand))]
    private string? _screenshotPath;
    [ObservableProperty]
    private ArtworkFrame? _tvFrame;

    /// <summary>
    /// Creates a new instance of the <see cref="ArtworkBuilderViewModel"/> class.
    /// </summary>
    /// <param name="composer">Draws the images.</param>
    /// <param name="dialogs">Screenshot picker and errors.</param>
    /// <param name="workFolder">Folder built images are written under.</param>
    public ArtworkBuilderViewModel(IArtworkComposer composer, IDialogService dialogs, Func<string> workFolder)
    {
        _composer = composer ?? throw new ArgumentNullException(nameof(composer));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _workFolder = workFolder ?? throw new ArgumentNullException(nameof(workFolder));
        PlayerChoices = new[] { "", "1", "2", "3", "4" };
    }

    /// <summary>
    /// Raised with the slot and the PNG it was built into.
    /// </summary>
    public event EventHandler<ArtworkBuiltEventArgs>? Built;

    /// <summary>
    /// Frames on offer for the GamePad boot screen; the same art as the TV.
    /// </summary>
    public ObservableCollection<ArtworkFrame> GamePadFrames { get; } = new();
    /// <summary>
    /// True once a screenshot is chosen; the logo needs none, the other slots do.
    /// </summary>
    public bool HasScreenshot => !string.IsNullOrWhiteSpace(ScreenshotPath);
    /// <summary>
    /// Frames on offer for the menu icon.
    /// </summary>
    public ObservableCollection<ArtworkFrame> IconFrames { get; } = new();
    /// <summary>
    /// Frames on offer for the boot logo.
    /// </summary>
    public ObservableCollection<ArtworkFrame> LogoFrames { get; } = new();
    /// <summary>
    /// Player counts to choose from; blank leaves the line off.
    /// </summary>
    public IReadOnlyList<string> PlayerChoices { get; }
    /// <summary>
    /// Frames on offer for the TV boot screen.
    /// </summary>
    public ObservableCollection<ArtworkFrame> TvFrames { get; } = new();

    /// <summary>
    /// True when the slot can be built: the logo always, the others once there is a screenshot.
    /// </summary>
    /// <param name="slot">Slot to test.</param>
    public bool CanBuild(ImageSlot? slot) => slot is not null && (slot == ImageSlot.BootLogo || HasScreenshot);

    /// <summary>
    /// Offers the frames for a console and seeds the text; keeps each frame choice when it still applies.
    /// </summary>
    /// <param name="console">Console being injected.</param>
    /// <param name="longName">Long name from the wizard, comma-separated lines.</param>
    /// <param name="shortName">Short name from the wizard, or null.</param>
    public void Refresh(SourceConsole console, string? longName, string? shortName = null)
    {
        TvFrame = Reload(TvFrames, ArtworkFrames.For(ImageSlot.BootTv, console), TvFrame);
        GamePadFrame = Reload(GamePadFrames, ArtworkFrames.For(ImageSlot.BootDrc, console), GamePadFrame);
        IconFrame = Reload(IconFrames, ArtworkFrames.For(ImageSlot.Icon, console), IconFrame);
        LogoFrame = Reload(LogoFrames, ArtworkFrames.For(ImageSlot.BootLogo, console), LogoFrame);

        var lines = (longName ?? string.Empty).Split(',').Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
        NameLine1 = lines.Length > 0 ? lines[0] : null;
        NameLine2 = lines.Length > 1 ? string.Join(" ", lines.Skip(1)) : null;
        LogoText = string.IsNullOrWhiteSpace(shortName) ? NameLine1 : shortName!.Trim();
    }

    /// <summary>
    /// What one slot should show right now.
    /// </summary>
    /// <param name="slot">Slot to describe.</param>
    private ArtworkRequest Request(ImageSlot slot) => new(FrameFor(slot))
    {
        ScreenshotPath = Clean(ScreenshotPath),
        NameLine1 = Clean(NameLine1),
        NameLine2 = Clean(NameLine2),
        LogoText = Clean(LogoText),
        ReleaseYear = int.TryParse(ReleaseYear, out var year) && year > 0 ? year : null,
        Players = int.TryParse(Players, out var players) && players > 0 ? players : null,
    };

    /// <summary>
    /// The frame chosen for a slot.
    /// </summary>
    /// <param name="slot">Slot to look up.</param>
    private ArtworkFrame? FrameFor(ImageSlot slot) =>
        slot == ImageSlot.BootTv ? TvFrame
        : slot == ImageSlot.BootDrc ? GamePadFrame
        : slot == ImageSlot.Icon ? IconFrame
        : LogoFrame;

    /// <summary>
    /// Draws one slot into a fresh file and hands it over; the other slots are untouched.
    /// </summary>
    /// <param name="slot">Slot to build.</param>
    [RelayCommand(CanExecute = nameof(CanBuild))]
    private async Task BuildAsync(ImageSlot? slot)
    {
        if (slot is null)
            return;

        var request = Request(slot);
        var path = Path.Combine(_workFolder(), "artwork", Guid.NewGuid().ToString("N"), slot.Name + ".png");
        try
        {
            IsBuilding = true;
            await Task.Run(() => _composer.ComposeAsync(request, slot, path)).ConfigureAwait(true);
            Built?.Invoke(this, new ArtworkBuiltEventArgs(slot, path));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            await _dialogs.ShowErrorAsync("Build " + slot.Name, e.Message).ConfigureAwait(true);
        }
        finally
        {
            IsBuilding = false;
        }
    }

    /// <summary>
    /// Lets the user pick the screenshot.
    /// </summary>
    [RelayCommand]
    private async Task PickScreenshotAsync()
    {
        var picked = await _dialogs.PickOpenFileAsync("Screenshot", ImageFilters).ConfigureAwait(true);
        if (picked is not null)
            ScreenshotPath = picked;
    }

    /// <summary>
    /// Trimmed text, or null when blank.
    /// </summary>
    /// <param name="text">Text to clean.</param>
    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text!.Trim();

    /// <summary>
    /// Refills a frame list and returns the choice to keep: the previous one when still offered, else the first.
    /// </summary>
    /// <param name="list">List to refill.</param>
    /// <param name="frames">Frames now on offer.</param>
    /// <param name="previous">Frame chosen before.</param>
    private static ArtworkFrame? Reload(ObservableCollection<ArtworkFrame> list, IReadOnlyList<ArtworkFrame> frames, ArtworkFrame? previous)
    {
        list.Clear();
        foreach (var frame in frames)
            list.Add(frame);

        return list.FirstOrDefault(f => f.Key == previous?.Key) ?? list.FirstOrDefault();
    }
}
