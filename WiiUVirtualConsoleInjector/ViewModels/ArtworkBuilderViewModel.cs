using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// Builds the icon, both boot screens and the boot logo from a screenshot: a frame per slot, captions, live previews, and applying the result to the slots.
/// </summary>
public sealed partial class ArtworkBuilderViewModel : ViewModelBase
{
    /// <summary>
    /// How long typing settles before the previews are redrawn.
    /// </summary>
    public static readonly TimeSpan PreviewDelay = TimeSpan.FromMilliseconds(200);

    private static readonly FileFilter[] ImageFilters = { new("Images", "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.webp", "*.tga"), new("All files", "*") };

    private readonly IArtworkComposer _composer;
    private readonly IDialogService _dialogs;
    private readonly IUiScheduler _scheduler;
    private readonly Func<string> _workFolder;

    private IDisposable? _pending;
    private int _renders;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    private ArtworkFrame? _gamePadFrame;
    [ObservableProperty]
    private string? _gamePadPreviewPath;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    private ArtworkFrame? _iconFrame;
    [ObservableProperty]
    private string? _iconPreviewPath;
    [ObservableProperty]
    private bool _isRendering;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    private ArtworkFrame? _logoFrame;
    [ObservableProperty]
    private string? _logoPreviewPath;
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
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    private string? _screenshotPath;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    private ArtworkFrame? _tvFrame;
    [ObservableProperty]
    private string? _tvPreviewPath;

    /// <summary>
    /// Creates a new instance of the <see cref="ArtworkBuilderViewModel"/> class.
    /// </summary>
    /// <param name="composer">Draws the images.</param>
    /// <param name="dialogs">Screenshot picker and errors.</param>
    /// <param name="scheduler">Debounces the previews.</param>
    /// <param name="workFolder">Folder previews and results are written under.</param>
    public ArtworkBuilderViewModel(IArtworkComposer composer, IDialogService dialogs, IUiScheduler scheduler, Func<string> workFolder)
    {
        _composer = composer ?? throw new ArgumentNullException(nameof(composer));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        _workFolder = workFolder ?? throw new ArgumentNullException(nameof(workFolder));
        PlayerChoices = new[] { "", "1", "2", "3", "4" };
    }

    /// <summary>
    /// Raised with the four image paths once a build is applied.
    /// </summary>
    public event EventHandler<ArtworkBuiltEventArgs>? Applied;

    /// <summary>
    /// True when there is enough to draw: a screenshot and a frame choice for every slot.
    /// </summary>
    public bool CanApply => !string.IsNullOrWhiteSpace(ScreenshotPath) && TvFrame is not null && GamePadFrame is not null && IconFrame is not null && LogoFrame is not null;
    /// <summary>
    /// Frames on offer for the GamePad boot screen; the same art as the TV.
    /// </summary>
    public ObservableCollection<ArtworkFrame> GamePadFrames { get; } = new();
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
    /// The preview render in flight, or a completed task.
    /// </summary>
    public Task PreviewRender { get; private set; } = Task.CompletedTask;
    /// <summary>
    /// Frames on offer for the TV boot screen.
    /// </summary>
    public ObservableCollection<ArtworkFrame> TvFrames { get; } = new();

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
        ScreenshotPath = string.IsNullOrWhiteSpace(ScreenshotPath) ? null : ScreenshotPath,
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
    /// Draws all four images into a fresh folder and hands them to the slots.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ApplyAsync()
    {
        var folder = Path.Combine(_workFolder(), "artwork", Guid.NewGuid().ToString("N"));
        var jobs = new[] { ImageSlot.Icon, ImageSlot.BootTv, ImageSlot.BootDrc, ImageSlot.BootLogo }
            .Select(slot => (Slot: slot, Request: Request(slot), Path: Path.Combine(folder, slot.Name + ".png")))
            .ToArray();
        try
        {
            IsRendering = true;
            await Task.Run(async () =>
            {
                foreach (var job in jobs)
                    await _composer.ComposeAsync(job.Request, job.Slot, job.Path).ConfigureAwait(false);
            }).ConfigureAwait(true);
            Applied?.Invoke(this, new ArtworkBuiltEventArgs(jobs[0].Path, jobs[1].Path, jobs[2].Path, jobs[3].Path));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            await _dialogs.ShowErrorAsync("Build artwork", e.Message).ConfigureAwait(true);
        }
        finally
        {
            IsRendering = false;
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
    /// Redraws the previews once the inputs have settled.
    /// </summary>
    private void SchedulePreview()
    {
        _pending?.Dispose();
        _pending = _scheduler.Delay(PreviewDelay, () => PreviewRender = RenderPreviewAsync());
    }

    /// <summary>
    /// Draws all four previews into the work folder; a newer render wins.
    /// </summary>
    private async Task RenderPreviewAsync()
    {
        var render = ++_renders;
        var folder = Path.Combine(_workFolder(), "artwork", "preview");
        var jobs = new[] { ImageSlot.BootTv, ImageSlot.BootDrc, ImageSlot.Icon, ImageSlot.BootLogo }
            .Select(slot => (Slot: slot, Request: Request(slot), Path: Path.Combine(folder, $"{slot.Name}-{render}.png")))
            .ToArray();
        try
        {
            IsRendering = true;
            await Task.Run(async () =>
            {
                foreach (var job in jobs)
                    await _composer.ComposeAsync(job.Request, job.Slot, job.Path).ConfigureAwait(false);
            }).ConfigureAwait(true);
            if (render != _renders)
                return;

            Forget(TvPreviewPath);
            Forget(GamePadPreviewPath);
            Forget(IconPreviewPath);
            Forget(LogoPreviewPath);
            TvPreviewPath = jobs[0].Path;
            GamePadPreviewPath = jobs[1].Path;
            IconPreviewPath = jobs[2].Path;
            LogoPreviewPath = jobs[3].Path;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            TvPreviewPath = null;
            GamePadPreviewPath = null;
            IconPreviewPath = null;
            LogoPreviewPath = null;
        }
        finally
        {
            if (render == _renders)
                IsRendering = false;
        }
    }

    /// <summary>
    /// Trimmed text, or null when blank.
    /// </summary>
    /// <param name="text">Text to clean.</param>
    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text!.Trim();

    /// <summary>
    /// Deletes a superseded preview file; a locked file is left for the work folder sweep.
    /// </summary>
    /// <param name="path">File to drop, or null.</param>
    private static void Forget(string? path)
    {
        if (path is null)
            return;

        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

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

    partial void OnGamePadFrameChanged(ArtworkFrame? value) => SchedulePreview();

    partial void OnIconFrameChanged(ArtworkFrame? value) => SchedulePreview();

    partial void OnLogoFrameChanged(ArtworkFrame? value) => SchedulePreview();

    partial void OnLogoTextChanged(string? value) => SchedulePreview();

    partial void OnNameLine1Changed(string? value) => SchedulePreview();

    partial void OnNameLine2Changed(string? value) => SchedulePreview();

    partial void OnPlayersChanged(string? value) => SchedulePreview();

    partial void OnReleaseYearChanged(string? value) => SchedulePreview();

    partial void OnScreenshotPathChanged(string? value) => SchedulePreview();

    partial void OnTvFrameChanged(ArtworkFrame? value) => SchedulePreview();
}
