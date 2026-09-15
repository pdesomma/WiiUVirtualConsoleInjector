using PD.WiiU.VirtualConsole;

namespace WiiUVirtualConsoleInjector.Services;

/// <summary>
/// The live <see cref="AppSettings"/> plus the paths they resolve to.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Raised after <see cref="Update"/> saves.
    /// </summary>
    event EventHandler? Changed;

    /// <summary>
    /// Base store folder: the setting, or the default under the data folder.
    /// </summary>
    string BasePath { get; }
    /// <summary>
    /// Settings as last saved.
    /// </summary>
    AppSettings Current { get; }
    /// <summary>
    /// Output folder: the setting, or the default under the data folder.
    /// </summary>
    string OutputPath { get; }
    /// <summary>
    /// Root of the SD card: the setting, or the drive that was detected; blank when there is neither.
    /// </summary>
    string SdPath { get; }
    /// <summary>
    /// Scratch folder: the setting, or the default under the data folder.
    /// </summary>
    string WorkPath { get; }

    /// <summary>
    /// Applies a change and saves.
    /// </summary>
    /// <param name="change">Produces the new settings from the current ones.</param>
    void Update(Func<AppSettings, AppSettings> change);
}
