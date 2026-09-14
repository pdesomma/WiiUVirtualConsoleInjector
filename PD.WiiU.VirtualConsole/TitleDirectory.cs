namespace PD.WiiU.VirtualConsole;

/// <summary>
/// Unpacked title on disk: code, content and meta folders under one root.
/// </summary>
public sealed class TitleDirectory
{
    /// <summary>
    /// Folder holding the executable, fw.img, app.xml and keys.
    /// </summary>
    public const string CodeFolder = "code";
    /// <summary>
    /// Folder holding the game data.
    /// </summary>
    public const string ContentFolder = "content";
    /// <summary>
    /// Folder holding meta.xml, images and the boot sound.
    /// </summary>
    public const string MetaFolder = "meta";

    /// <summary>
    /// Creates a new instance of the <see cref="TitleDirectory"/> class.
    /// </summary>
    /// <param name="root">Folder containing the three subfolders.</param>
    public TitleDirectory(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));

        Root = Path.GetFullPath(root);
    }

    /// <summary>
    /// Path of app.xml.
    /// </summary>
    public string AppXmlPath => Path.Combine(Code, "app.xml");
    /// <summary>
    /// Code folder path.
    /// </summary>
    public string Code => Path.Combine(Root, CodeFolder);
    /// <summary>
    /// Content folder path.
    /// </summary>
    public string Content => Path.Combine(Root, ContentFolder);
    /// <summary>
    /// True when all three subfolders exist.
    /// </summary>
    public bool Exists => Directory.Exists(Code) && Directory.Exists(Content) && Directory.Exists(Meta);
    /// <summary>
    /// Meta folder path.
    /// </summary>
    public string Meta => Path.Combine(Root, MetaFolder);
    /// <summary>
    /// Path of meta.xml.
    /// </summary>
    public string MetaXmlPath => Path.Combine(Meta, "meta.xml");
    /// <summary>
    /// Root folder path.
    /// </summary>
    public string Root { get; }

    /// <summary>
    /// Creates the three subfolders under the root.
    /// </summary>
    /// <param name="root">Folder to create under.</param>
    public static TitleDirectory Create(string root)
    {
        var title = new TitleDirectory(root);
        Directory.CreateDirectory(title.Code);
        Directory.CreateDirectory(title.Content);
        Directory.CreateDirectory(title.Meta);
        return title;
    }

    /// <inheritdoc/>
    public override string ToString() => Root;
}
