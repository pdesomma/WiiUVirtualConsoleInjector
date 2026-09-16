using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Ports;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// The previous application's keys, bases and settings: what was found on this machine, and one button to take it all.
/// </summary>
public sealed partial class LegacyImportViewModel : ViewModelBase
{
    private readonly IDialogService _dialogs;
    private readonly ILegacyImport _import;
    private readonly ISettingsService _settings;
    private readonly IToastService _toasts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasInstall), nameof(Summary), nameof(Location), nameof(Heading))]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    private LegacyInstall? _install;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportCommand))]
    private bool _isRunning;
    [ObservableProperty]
    private string? _progress;
    [ObservableProperty]
    private string? _status;

    /// <summary>
    /// Creates a new instance of the <see cref="LegacyImportViewModel"/> class.
    /// </summary>
    /// <param name="import">Finds and takes the previous application's data.</param>
    /// <param name="settings">Takes its output folder and silenced warnings.</param>
    /// <param name="dialogs">Error boxes.</param>
    /// <param name="toasts">The one-time pointer at startup.</param>
    public LegacyImportViewModel(ILegacyImport import, ISettingsService settings, IDialogService dialogs, IToastService toasts)
    {
        _import = import ?? throw new ArgumentNullException(nameof(import));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _toasts = toasts ?? throw new ArgumentNullException(nameof(toasts));
    }

    /// <summary>
    /// Raised after an import so pages showing keys, bases and settings can re-read them.
    /// </summary>
    public event EventHandler? Imported;

    /// <summary>
    /// Whether it is here and whether there is anything to import.
    /// </summary>
    public string Heading => HasInstall ? "UWUVCI AIO was found on this machine. Keys and downloaded bases can be imported." : "UWUVCI AIO was not found on this machine.";
    /// <summary>
    /// True when the previous application left something to take.
    /// </summary>
    public bool HasInstall => Install is not null;
    /// <summary>
    /// Where it was recognised.
    /// </summary>
    public string Location => Install?.Location ?? "";
    /// <summary>
    /// What was found, in one line.
    /// </summary>
    public string Summary
    {
        get
        {
            if (Install is not { } i)
                return "";
            var parts = new List<string>();
            if (i.CommonKey is not null)
                parts.Add("the Wii U common key");
            if (i.TitleKeys.Count > 0)
                parts.Add(i.TitleKeys.Count == 1 ? "1 title key" : $"{i.TitleKeys.Count} title keys");
            if (i.Bases.Count > 0)
                parts.Add(i.Bases.Count == 1 ? "1 downloaded base" : $"{i.Bases.Count} downloaded bases");
            if (i.OutputFolder is not null)
                parts.Add("its output folder");
            if (i.SuppressedWarnings.Count > 0)
                parts.Add("which warnings were turned off");
            return "Found " + string.Join(", ", parts) + ".";
        }
    }

    /// <summary>
    /// Looks for the previous application again.
    /// </summary>
    public void Refresh() => Install = _import.Find();

    /// <summary>
    /// The startup pointer: once, when there is something to take and it has not been mentioned before.
    /// </summary>
    public void OfferAtStartup()
    {
        Refresh();
        if (Install is null || _settings.Current.LegacyImportOffered)
            return;
        _settings.Update(s => s with { LegacyImportOffered = true });
        _toasts.Show(ToastKind.Info, "UWUVCI AIO found", "Its keys and downloaded bases can be imported on Settings.");
    }

    private bool CanImport() => HasInstall && !IsRunning;

    /// <summary>
    /// Takes everything not already present; the output folder only when none has been chosen here.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanImport))]
    private async Task ImportAsync()
    {
        if (Install is not { } install)
            return;
        IsRunning = true;
        Status = null;
        try
        {
            var report = await _import.ImportAsync(install, new Progress<string>(m => Progress = m));
            var settingsTaken = new List<string>();
            _settings.Update(s =>
            {
                var next = s;
                if (install.OutputFolder is not null && s.OutputPath is null)
                {
                    next = next with { OutputPath = install.OutputFolder };
                    settingsTaken.Add("output folder");
                }
                foreach (var warning in install.SuppressedWarnings)
                    next = next.Suppress(warning);
                return next with { LegacyImportOffered = true };
            });
            var parts = new List<string>();
            if (report.CommonKeyAdded)
                parts.Add("the Wii U common key");
            if (report.TitleKeysAdded > 0)
                parts.Add(report.TitleKeysAdded == 1 ? "1 title key" : $"{report.TitleKeysAdded} title keys");
            if (report.BasesAdded > 0)
                parts.Add(report.BasesAdded == 1 ? "1 base" : $"{report.BasesAdded} bases");
            parts.AddRange(settingsTaken);
            Status = parts.Count == 0 ? "Nothing to import; everything was already here." : "Imported " + string.Join(", ", parts) + ".";
            if (report.Failures.Count > 0)
                Status += " Could not copy: " + string.Join("; ", report.Failures);
            Imported?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
        {
            await _dialogs.ShowErrorAsync("Import", e.Message);
        }
        finally
        {
            Progress = null;
            IsRunning = false;
        }
    }
}
