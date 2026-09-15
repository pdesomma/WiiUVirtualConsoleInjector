using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// One artwork slot built on its own: its own source image, its own overlay, its own text, its own Build.
/// </summary>
public sealed partial class ArtworkSlotViewModel : ViewModelBase
{
    private static readonly FileFilter[] ImageFilters = { new("Images", "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.webp", "*.tga"), new("All files", "*") };

    private readonly IArtworkComposer _composer;
    private readonly IDialogService _dialogs;
    private readonly Func<string> _workFolder;

    [ObservableProperty]
    private bool _isBuilding;
    [ObservableProperty]
    private string? _logoText;
    [ObservableProperty]
    private string? _nameLine1;
    [ObservableProperty]
    private string? _nameLine2;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BuildCommand))]
    private ArtworkFrame? _overlay;
    [ObservableProperty]
    private string? _players;
    [ObservableProperty]
    private string? _releaseYear;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSource))]
    [NotifyCanExecuteChangedFor(nameof(BuildCommand))]
    private string? _sourcePath;

    /// <summary>
    /// Creates a new instance of the <see cref="ArtworkSlotViewModel"/> class.
    /// </summary>
    /// <param name="slot">Slot this builds.</param>
    /// <param name="composer">Draws the image.</param>
    /// <param name="dialogs">Image picker and errors.</param>
    /// <param name="workFolder">Folder built images are written under.</param>
    public ArtworkSlotViewModel(ImageSlot slot, IArtworkComposer composer, IDialogService dialogs, Func<string> workFolder)
    {
        Slot = slot ?? throw new ArgumentNullException(nameof(slot));
        _composer = composer ?? throw new ArgumentNullException(nameof(composer));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _workFolder = workFolder ?? throw new ArgumentNullException(nameof(workFolder));
        PlayerChoices = new[] { "", "1", "2", "3", "4" };
    }

    /// <summary>
    /// Raised with the PNG the slot was built into.
    /// </summary>
    public event EventHandler<ArtworkBuiltEventArgs>? Built;

    /// <summary>
    /// True when the slot can be built: the logo always, the others once there is a source image.
    /// </summary>
    public bool CanBuild => Overlay is not null && (IsLogo || HasSource);
    /// <summary>
    /// True once a source image is chosen.
    /// </summary>
    public bool HasSource => !string.IsNullOrWhiteSpace(SourcePath);
    /// <summary>
    /// True for the two boot screens, which carry the name, year and player captions.
    /// </summary>
    public bool IsBootScreen => Slot == ImageSlot.BootTv || Slot == ImageSlot.BootDrc;
    /// <summary>
    /// True for the boot logo, which carries text instead of an image.
    /// </summary>
    public bool IsLogo => Slot == ImageSlot.BootLogo;
    /// <summary>
    /// Overlays on offer for this slot.
    /// </summary>
    public ObservableCollection<ArtworkFrame> Overlays { get; } = new();
    /// <summary>
    /// Player counts to choose from; blank leaves the line off.
    /// </summary>
    public IReadOnlyList<string> PlayerChoices { get; }
    /// <summary>
    /// Slot this builds.
    /// </summary>
    public ImageSlot Slot { get; }

    /// <summary>
    /// Forgets the source image and every caption; the overlay choice stays.
    /// </summary>
    public void Clear()
    {
        SourcePath = null;
        NameLine1 = null;
        NameLine2 = null;
        ReleaseYear = null;
        Players = null;
        LogoText = null;
    }

    /// <summary>
    /// Offers the overlays for a console; keeps the current one when it still applies.
    /// </summary>
    /// <param name="console">Console being injected.</param>
    public void Refresh(SourceConsole console)
    {
        var previous = Overlay?.Key;
        Overlays.Clear();
        foreach (var frame in ArtworkFrames.For(Slot, console))
            Overlays.Add(frame);

        Overlay = Overlays.FirstOrDefault(f => f.Key == previous) ?? Overlays.FirstOrDefault();
    }

    /// <summary>
    /// Fills in captions that are still blank from the wizard's names.
    /// </summary>
    /// <param name="longName">Long name, comma-separated lines.</param>
    /// <param name="shortName">Short name, or null.</param>
    public void Seed(string? longName, string? shortName)
    {
        var lines = (longName ?? string.Empty).Split(',').Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
        if (string.IsNullOrWhiteSpace(NameLine1) && lines.Length > 0)
            NameLine1 = lines[0];
        if (string.IsNullOrWhiteSpace(NameLine2) && lines.Length > 1)
            NameLine2 = string.Join(" ", lines.Skip(1));
        if (string.IsNullOrWhiteSpace(LogoText))
            LogoText = Clean(shortName) ?? (lines.Length > 0 ? lines[0] : null);
    }

    /// <summary>
    /// What this slot should show right now.
    /// </summary>
    private ArtworkRequest Request() => new(Overlay)
    {
        ScreenshotPath = Clean(SourcePath),
        NameLine1 = Clean(NameLine1),
        NameLine2 = Clean(NameLine2),
        LogoText = Clean(LogoText),
        ReleaseYear = int.TryParse(ReleaseYear, out var year) && year > 0 ? year : null,
        Players = int.TryParse(Players, out var players) && players > 0 ? players : null,
    };

    /// <summary>
    /// Draws the slot into a fresh file and hands it over.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanBuild))]
    private async Task BuildAsync()
    {
        var request = Request();
        var path = Path.Combine(_workFolder(), "artwork", Guid.NewGuid().ToString("N"), Slot.Name + ".png");
        try
        {
            IsBuilding = true;
            await Task.Run(() => _composer.ComposeAsync(request, Slot, path)).ConfigureAwait(true);
            Built?.Invoke(this, new ArtworkBuiltEventArgs(Slot, path));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            await _dialogs.ShowErrorAsync("Build " + Slot.Name, e.Message).ConfigureAwait(true);
        }
        finally
        {
            IsBuilding = false;
        }
    }

    /// <summary>
    /// Lets the user pick the source image.
    /// </summary>
    [RelayCommand]
    private async Task PickSourceAsync()
    {
        var picked = await _dialogs.PickOpenFileAsync(Slot.Name + " source image", ImageFilters).ConfigureAwait(true);
        if (picked is not null)
            SourcePath = picked;
    }

    /// <summary>
    /// Trimmed text, or null when blank.
    /// </summary>
    /// <param name="text">Text to clean.</param>
    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text!.Trim();
}
