using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Ports;
using WiiUSharp;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// Builds the icon and boot screens from a screenshot: frame, captions, a live preview, and applying the result to the artwork slots.
/// </summary>
public sealed partial class ArtworkBuilderViewModel : ViewModelBase
{
    /// <summary>
    /// How long typing settles before the preview is redrawn.
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
    private string? _iconPreviewPath;
    [ObservableProperty]
    private bool _isRendering;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    private string? _nameLine1;
    [ObservableProperty]
    private string? _nameLine2;
    [ObservableProperty]
    private string? _players;
    [ObservableProperty]
    private string? _previewPath;
    [ObservableProperty]
    private string? _releaseYear;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    private string? _screenshotPath;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyCommand))]
    private ArtworkTemplate? _selectedTemplate;

    /// <summary>
    /// Creates a new instance of the <see cref="ArtworkBuilderViewModel"/> class.
    /// </summary>
    /// <param name="composer">Draws the images.</param>
    /// <param name="dialogs">Screenshot picker and errors.</param>
    /// <param name="scheduler">Debounces the preview.</param>
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
    /// Raised with the icon, TV and GamePad paths once a build is applied.
    /// </summary>
    public event EventHandler<ArtworkBuiltEventArgs>? Applied;

    /// <summary>
    /// True when there is enough to draw something.
    /// </summary>
    public bool CanApply => SelectedTemplate is not null && !string.IsNullOrWhiteSpace(ScreenshotPath);
    /// <summary>
    /// Player counts to choose from; blank leaves the line off.
    /// </summary>
    public IReadOnlyList<string> PlayerChoices { get; }
    /// <summary>
    /// The preview render in flight, or a completed task.
    /// </summary>
    public Task PreviewRender { get; private set; } = Task.CompletedTask;
    /// <summary>
    /// Frames for the console being injected.
    /// </summary>
    public ObservableCollection<ArtworkTemplate> Templates { get; } = new();

    /// <summary>
    /// Offers the frames for a console and seeds the name lines; keeps the frame when it still applies.
    /// </summary>
    /// <param name="console">Console being injected.</param>
    /// <param name="longName">Long name from the wizard, comma-separated lines.</param>
    public void Refresh(SourceConsole console, string? longName)
    {
        var previous = SelectedTemplate?.Key;
        Templates.Clear();
        foreach (var template in ArtworkTemplates.For(console))
            Templates.Add(template);
        SelectedTemplate = Templates.FirstOrDefault(t => t.Key == previous) ?? Templates.FirstOrDefault();

        var lines = (longName ?? string.Empty).Split(',').Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
        NameLine1 = lines.Length > 0 ? lines[0] : null;
        NameLine2 = lines.Length > 1 ? string.Join(" ", lines.Skip(1)) : null;
    }

    /// <summary>
    /// The images as the inputs describe them right now.
    /// </summary>
    private ArtworkRequest Request() => new(SelectedTemplate!)
    {
        ScreenshotPath = string.IsNullOrWhiteSpace(ScreenshotPath) ? null : ScreenshotPath,
        NameLine1 = string.IsNullOrWhiteSpace(NameLine1) ? null : NameLine1!.Trim(),
        NameLine2 = string.IsNullOrWhiteSpace(NameLine2) ? null : NameLine2!.Trim(),
        ReleaseYear = int.TryParse(ReleaseYear, out var year) && year > 0 ? year : null,
        Players = int.TryParse(Players, out var players) && players > 0 ? players : null,
    };

    /// <summary>
    /// Draws all three images into a fresh folder and hands them to the slots.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ApplyAsync()
    {
        var folder = Path.Combine(_workFolder(), "artwork", Guid.NewGuid().ToString("N"));
        var request = Request();
        try
        {
            IsRendering = true;
            var icon = Path.Combine(folder, "iconTex.png");
            var tv = Path.Combine(folder, "bootTvTex.png");
            var drc = Path.Combine(folder, "bootDrcTex.png");
            await Task.Run(async () =>
            {
                await _composer.ComposeAsync(request, ImageSlot.Icon, icon).ConfigureAwait(false);
                await _composer.ComposeAsync(request, ImageSlot.BootTv, tv).ConfigureAwait(false);
                await _composer.ComposeAsync(request, ImageSlot.BootDrc, drc).ConfigureAwait(false);
            }).ConfigureAwait(true);
            Applied?.Invoke(this, new ArtworkBuiltEventArgs(icon, tv, drc));
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
        if (SelectedTemplate is null)
        {
            PreviewPath = null;
            IconPreviewPath = null;
            return;
        }

        _pending = _scheduler.Delay(PreviewDelay, () => PreviewRender = RenderPreviewAsync());
    }

    /// <summary>
    /// Draws the TV screen and icon previews into the work folder; a newer render wins.
    /// </summary>
    private async Task RenderPreviewAsync()
    {
        var render = ++_renders;
        var request = Request();
        var folder = Path.Combine(_workFolder(), "artwork", "preview");
        var tv = Path.Combine(folder, $"tv-{render}.png");
        var icon = Path.Combine(folder, $"icon-{render}.png");
        try
        {
            IsRendering = true;
            await Task.Run(async () =>
            {
                await _composer.ComposeAsync(request, ImageSlot.BootTv, tv).ConfigureAwait(false);
                await _composer.ComposeAsync(request, ImageSlot.Icon, icon).ConfigureAwait(false);
            }).ConfigureAwait(true);
            if (render != _renders)
                return;

            var oldTv = PreviewPath;
            var oldIcon = IconPreviewPath;
            PreviewPath = tv;
            IconPreviewPath = icon;
            Forget(oldTv);
            Forget(oldIcon);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            PreviewPath = null;
            IconPreviewPath = null;
        }
        finally
        {
            if (render == _renders)
                IsRendering = false;
        }
    }

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

    partial void OnNameLine1Changed(string? value) => SchedulePreview();

    partial void OnNameLine2Changed(string? value) => SchedulePreview();

    partial void OnPlayersChanged(string? value) => SchedulePreview();

    partial void OnReleaseYearChanged(string? value) => SchedulePreview();

    partial void OnScreenshotPathChanged(string? value) => SchedulePreview();

    partial void OnSelectedTemplateChanged(ArtworkTemplate? value) => SchedulePreview();
}
