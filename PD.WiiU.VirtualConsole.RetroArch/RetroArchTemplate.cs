namespace PD.WiiU.VirtualConsole.RetroArch;

/// <summary>
/// The embedded title skeleton a core is dropped into: full-permission cos.xml, app.xml and meta.xml with placeholder identity, and stock boot art.
/// </summary>
public static class RetroArchTemplate
{
    /// <summary>
    /// Where a title's content folder is mounted on the console, as the core sees it.
    /// </summary>
    public const string ContentMount = "fs:/vol/content/";
    /// <summary>
    /// Prefix of every template resource.
    /// </summary>
    public const string ResourcePrefix = "template/";

    /// <summary>
    /// Files under code, as resource paths relative to <see cref="ResourcePrefix"/>.
    /// </summary>
    public static readonly IReadOnlyList<string> CodeFiles = new[] { "code/app.xml", "code/cos.xml" };

    /// <summary>
    /// Files under meta, as resource paths relative to <see cref="ResourcePrefix"/>.
    /// </summary>
    public static readonly IReadOnlyList<string> MetaFiles = new[]
    {
        "meta/meta.xml", "meta/iconTex.tga", "meta/bootTvTex.tga", "meta/bootDrcTex.tga", "meta/bootLogoTex.tga", "meta/bootMovie.h264", "meta/bootSound.btsnd",
    };

    /// <summary>
    /// Writes every template file under the title root.
    /// </summary>
    /// <param name="title">Title to fill; its folders are created.</param>
    /// <param name="cancellationToken">Cancels between files.</param>
    public static async Task WriteAsync(TitleDirectory title, CancellationToken cancellationToken = default)
    {
        if (title is null)
            throw new ArgumentNullException(nameof(title));

        TitleDirectory.Create(title.Root);
        foreach (var file in CodeFiles.Concat(MetaFiles))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var target = Path.Combine(title.Root, file.Replace('/', Path.DirectorySeparatorChar));
            using var source = EmbeddedResources.Open(ResourcePrefix + file);
            using var destination = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None);
            await source.CopyToAsync(destination, 81920, cancellationToken).ConfigureAwait(false);
        }
    }
}
