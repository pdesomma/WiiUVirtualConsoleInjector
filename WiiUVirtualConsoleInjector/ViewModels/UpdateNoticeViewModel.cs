using System.Net.Http;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PD.WiiU.VirtualConsole;
using PD.WiiU.VirtualConsole.Ports;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// Whether a newer release is out: looked up once a day at startup when allowed, or on demand from Settings, and shown in the header until opened or dismissed.
/// </summary>
public sealed partial class UpdateNoticeViewModel : ViewModelBase
{
    /// <summary>
    /// How long a startup check is skipped after the last one.
    /// </summary>
    public static readonly TimeSpan Throttle = TimeSpan.FromHours(20);

    private readonly IUpdateCheck _check;
    private readonly ILinkOpener _links;
    private readonly Func<DateTimeOffset> _now;
    private readonly ISettingsService _settings;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAvailable), nameof(Text))]
    private AppRelease? _available;
    [ObservableProperty]
    private bool _isChecking;
    [ObservableProperty]
    private string? _status;

    /// <summary>
    /// Creates a new instance of the <see cref="UpdateNoticeViewModel"/> class.
    /// </summary>
    /// <param name="check">Where the newest release is found.</param>
    /// <param name="settings">Remembers the last check and whether to check at all.</param>
    /// <param name="links">Opens the release page.</param>
    /// <param name="current">The running version.</param>
    /// <param name="now">Clock, or null for the system's.</param>
    public UpdateNoticeViewModel(IUpdateCheck check, ISettingsService settings, ILinkOpener links, Version current, Func<DateTimeOffset>? now = null)
    {
        _check = check ?? throw new ArgumentNullException(nameof(check));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _links = links ?? throw new ArgumentNullException(nameof(links));
        Current = current ?? throw new ArgumentNullException(nameof(current));
        _now = now ?? (() => DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Look for a release when the application starts.
    /// </summary>
    public bool CheckAtStartup
    {
        get => _settings.Current.CheckForUpdates;
        set
        {
            if (value == _settings.Current.CheckForUpdates)
                return;
            _settings.Update(s => s with { CheckForUpdates = value });
            OnPropertyChanged();
        }
    }
    /// <summary>
    /// The running version.
    /// </summary>
    public Version Current { get; }
    /// <summary>
    /// The running version as shown.
    /// </summary>
    public string CurrentText => $"Version {Trim(Current)}";
    /// <summary>
    /// True when a newer release is known and not yet dismissed.
    /// </summary>
    public bool IsAvailable => Available is not null;
    /// <summary>
    /// The header line, or empty.
    /// </summary>
    public string Text => Available is { } r ? $"Version {Trim(r.Version)} is out" : "";

    /// <summary>
    /// The startup check: skipped when turned off or done recently; never throws.
    /// </summary>
    public Task CheckAtStartupAsync()
    {
        if (!_settings.Current.CheckForUpdates)
            return Task.CompletedTask;
        if (_settings.Current.LastUpdateCheck is { } last && _now() - last < Throttle)
            return Task.CompletedTask;
        return CheckAsync(quiet: true);
    }

    /// <summary>
    /// Asks for the newest release and remembers when; quiet swallows failures and only speaks up for a newer version.
    /// </summary>
    /// <param name="quiet">True at startup.</param>
    private async Task CheckAsync(bool quiet)
    {
        IsChecking = true;
        try
        {
            var release = await _check.LatestAsync();
            _settings.Update(s => s with { LastUpdateCheck = _now() });
            if (release is { } r && r.IsNewerThan(Current))
            {
                Available = r;
                Status = Text + ".";
            }
            else
            {
                Available = null;
                Status = quiet ? null : release is null ? "Nothing has been published yet." : "This is the newest version.";
            }
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or IOException)
        {
            Status = quiet ? null : "Could not reach GitHub: " + e.Message;
        }
        finally
        {
            IsChecking = false;
        }
    }

    private static string Trim(Version v) => v.Build >= 0 ? v.ToString(3) : v.ToString(2);

    /// <summary>
    /// The on-demand check from Settings.
    /// </summary>
    [RelayCommand]
    private Task CheckNowAsync() => CheckAsync(quiet: false);

    /// <summary>
    /// Hides the notice until the next check.
    /// </summary>
    [RelayCommand]
    private void Dismiss() => Available = null;

    /// <summary>
    /// Opens the release page.
    /// </summary>
    [RelayCommand]
    private Task OpenAsync() => Available is { } r ? _links.OpenAsync(r.Page) : Task.FromResult(false);
}
