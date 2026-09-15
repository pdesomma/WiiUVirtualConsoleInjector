using PD.WiiU.VirtualConsole;
using WiiUVirtualConsoleInjector.Services;

namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// One injection warning and whether it is still shown.
/// </summary>
public sealed class WarningSettingViewModel : ViewModelBase
{
    private readonly ISettingsService _settings;

    /// <summary>
    /// Creates a new instance of the <see cref="WarningSettingViewModel"/> class.
    /// </summary>
    /// <param name="warning">Warning this row controls.</param>
    /// <param name="settings">Where suppression is saved.</param>
    public WarningSettingViewModel(InjectionWarning warning, ISettingsService settings)
    {
        Warning = warning;
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        Description = Describe(warning);
    }

    /// <summary>
    /// What the warning says.
    /// </summary>
    public string Description { get; }
    /// <summary>
    /// True while the warning is still shown before an inject; saves on change.
    /// </summary>
    public bool ShowAgain
    {
        get => !_settings.Current.IsSuppressed(Warning);
        set
        {
            if (value == ShowAgain)
                return;

            _settings.Update(s => value ? s.Restore(Warning) : s.Suppress(Warning));
            OnPropertyChanged();
        }
    }
    /// <summary>
    /// Warning this row controls.
    /// </summary>
    public InjectionWarning Warning { get; }

    /// <summary>
    /// Re-reads the state after settings changed elsewhere.
    /// </summary>
    public void Refresh() => OnPropertyChanged(nameof(ShowAgain));

    /// <summary>
    /// User-facing text for a warning.
    /// </summary>
    /// <param name="warning">Warning to describe.</param>
    private static string Describe(InjectionWarning warning) => warning switch
    {
        InjectionWarning.NdsDsiEnhanced => "DSi-enhanced NDS ROMs do not run",
        InjectionWarning.SnesCoProcessor => "SNES ROMs that need a co-processor do not run",
        InjectionWarning.GameCubeGcz => "GCZ images take longer and use more space than ISO",
        _ => warning.ToString(),
    };
}
