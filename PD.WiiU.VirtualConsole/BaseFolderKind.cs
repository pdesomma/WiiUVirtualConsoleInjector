namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Shapes a base can arrive in.
/// </summary>
public enum BaseFolderKind
{
    /// <summary>
    /// code, content and meta folders, as unpacked.
    /// </summary>
    Title,
    /// <summary>
    /// title.tmd, title.tik and encrypted contents, as installable.
    /// </summary>
    Package,
}
