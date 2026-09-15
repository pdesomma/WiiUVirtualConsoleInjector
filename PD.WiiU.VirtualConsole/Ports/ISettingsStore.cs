namespace PD.WiiU.VirtualConsole.Ports;

/// <summary>
/// Where <see cref="AppSettings"/> persist.
/// </summary>
public interface ISettingsStore
{
    /// <summary>
    /// Reads the saved settings, or defaults when nothing usable is saved.
    /// </summary>
    AppSettings Load();

    /// <summary>
    /// Writes the settings; the previous file is replaced whole.
    /// </summary>
    /// <param name="settings">Settings to keep.</param>
    void Save(AppSettings settings);
}
