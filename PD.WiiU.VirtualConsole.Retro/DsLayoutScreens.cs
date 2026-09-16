using System.IO.Compression;
using PD.WiiU.VirtualConsole.Options;

namespace PD.WiiU.VirtualConsole.Retro;

/// <summary>
/// The bundled extra DS screen layouts, laid over a title's content.
/// </summary>
public static class DsLayoutScreens
{
    private const string ResourceName = "ds_layout_screens.zip";

    /// <summary>
    /// Copies one pack's files over the title, replacing what the base had.
    /// </summary>
    /// <param name="pack">Which set.</param>
    /// <param name="titleRoot">Title folder holding content/.</param>
    /// <returns>Files written.</returns>
    public static int Extract(NdsLayoutPack pack, string titleRoot)
    {
        if (titleRoot is null)
            throw new ArgumentNullException(nameof(titleRoot));
        if (pack == NdsLayoutPack.None)
            return 0;

        var prefix = Folder(pack) + "/";
        using var stream = typeof(DsLayoutScreens).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded {ResourceName} is missing.");
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        var written = 0;
        foreach (var entry in zip.Entries)
        {
            if (!entry.FullName.StartsWith(prefix, StringComparison.Ordinal) || entry.FullName.EndsWith("/", StringComparison.Ordinal))
                continue;
            var relative = entry.FullName.Substring(prefix.Length).Replace('/', Path.DirectorySeparatorChar);
            var path = Path.Combine(titleRoot, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using var input = entry.Open();
            using var output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            input.CopyTo(output);
            written++;
        }
        return written;
    }

    /// <summary>
    /// The pack's folder inside the archive; the misspelling is the original's.
    /// </summary>
    private static string Folder(NdsLayoutPack pack) => pack switch
    {
        NdsLayoutPack.All => "All",
        NdsLayoutPack.PhantomHourglass => "Phatnom Hourglass",
        _ => throw new ArgumentOutOfRangeException(nameof(pack), pack, "Unknown layout pack."),
    };
}
