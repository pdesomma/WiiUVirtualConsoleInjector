namespace PD.WiiU.VirtualConsole.RetroArch;

/// <summary>
/// Settings appended over the card's retroarch.cfg when a title starts: the start-up notifications off, and no save on exit so they never land in the card's own config.
/// </summary>
public static class RetroArchTitleConfig
{
    /// <summary>
    /// Name of the file under content.
    /// </summary>
    public const string FileName = "retroarch.cfg";
    /// <summary>
    /// The RetroArch switch that loads it.
    /// </summary>
    public const string Switch = "--appendconfig";

    /// <summary>
    /// Every key written, in order.
    /// </summary>
    public static readonly IReadOnlyList<KeyValuePair<string, string>> Settings = new[]
    {
        new KeyValuePair<string, string>("config_save_on_exit", "false"),
        new KeyValuePair<string, string>("menu_show_load_content_animation", "false"),
        new KeyValuePair<string, string>("notification_show_autoconfig", "false"),
        new KeyValuePair<string, string>("notification_show_autoconfig_fails", "false"),
        new KeyValuePair<string, string>("notification_show_cheats_applied", "false"),
        new KeyValuePair<string, string>("notification_show_config_override_load", "false"),
        new KeyValuePair<string, string>("notification_show_patch_applied", "false"),
        new KeyValuePair<string, string>("notification_show_refresh_rate", "false"),
        new KeyValuePair<string, string>("notification_show_remap_load", "false"),
        new KeyValuePair<string, string>("notification_show_set_initial_disk", "false"),
    };

    /// <summary>
    /// The file's text: one <c>key = "value"</c> per line, LF-terminated as RetroArch writes its own.
    /// </summary>
    public static string Text => string.Concat(Settings.Select(s => $"{s.Key} = \"{s.Value}\"\n"));

    /// <summary>
    /// Writes the file under the title's content folder.
    /// </summary>
    /// <param name="title">Title being built.</param>
    /// <returns>Path of the written file.</returns>
    public static string Write(TitleDirectory title)
    {
        if (title is null)
            throw new ArgumentNullException(nameof(title));
        var path = Path.Combine(title.Content, FileName);
        File.WriteAllText(path, Text);
        return path;
    }
}
