namespace PD.WiiU.VirtualConsole.Infrastructure;

/// <summary>
/// Finds the font the boot screens are captioned in: the one chosen, else the previous application's copy, else nothing.
/// </summary>
public static class CaptionFont
{
    /// <summary>
    /// File name the previous application kept its caption font under.
    /// </summary>
    public const string LegacyFileName = "font.otf";

    /// <summary>
    /// The font file to use, or null to fall back to the bundled UI font.
    /// </summary>
    /// <param name="chosen">Path from settings, or null.</param>
    /// <param name="candidates">Folders to look in when nothing is chosen; the previous application's tool folders.</param>
    public static string? Locate(string? chosen, IEnumerable<string> candidates)
    {
        if (candidates is null)
            throw new ArgumentNullException(nameof(candidates));
        if (!string.IsNullOrWhiteSpace(chosen))
            return File.Exists(chosen) ? chosen : null;

        return candidates
            .Select(folder => Path.Combine(folder, LegacyFileName))
            .FirstOrDefault(File.Exists);
    }

    /// <summary>
    /// Where the previous application kept its tools on this machine.
    /// </summary>
    public static IEnumerable<string> LegacyToolFolders()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (documents.Length > 0)
            yield return Path.Combine(documents, "UWUVCI AIO", "bin", "Tools");

        yield return Path.Combine(AppContext.BaseDirectory, "bin", "Tools");
    }
}
