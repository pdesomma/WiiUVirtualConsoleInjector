using GMWare.M2.MArchive;
using GMWare.M2.Psb;
using Newtonsoft.Json.Linq;

namespace PD.WiiU.VirtualConsole.Gba;

/// <summary>
/// The M2 archive a GBA base keeps its ROM and settings in: content/alldata.psb.m plus alldata.bin.
/// </summary>
public static class AllDataArchive
{
    /// <summary>
    /// Base name of the archive files.
    /// </summary>
    public const string BaseName = "alldata";
    /// <summary>
    /// Key period the Wii U titles use.
    /// </summary>
    public const int KeyLength = 80;
    /// <summary>
    /// Manifest file name.
    /// </summary>
    public const string ManifestFileName = "alldata.psb.m";
    /// <summary>
    /// Encryption seed the Wii U titles use.
    /// </summary>
    public const string Seed = "MX8wgGEJ2+M47";
    /// <summary>
    /// Suffix of an entry that is MDF-compressed with the title key.
    /// </summary>
    public const string CompressedSuffix = ".m";
    /// <summary>
    /// Folder inside the archive that holds the ROM, e.g. system/roms/AA88E0.D88.m.
    /// </summary>
    public const string RomFolder = "system/roms";
    /// <summary>
    /// Settings file inside the archive, relative to its root.
    /// </summary>
    public const string TitleProfileName = "title_prof.psb.m";

    /// <summary>
    /// Unpacks the archive, lets the caller edit the files, and rebuilds it.
    /// </summary>
    /// <param name="contentDirectory">Folder holding alldata.psb.m and alldata.bin.</param>
    /// <param name="workDirectory">Scratch folder; created and removed here.</param>
    /// <param name="edit">Receives the unpacked root.</param>
    /// <exception cref="FileNotFoundException">No manifest in the content folder.</exception>
    public static void Rewrite(string contentDirectory, string workDirectory, Action<string> edit)
    {
        if (contentDirectory is null)
            throw new ArgumentNullException(nameof(contentDirectory));
        if (workDirectory is null)
            throw new ArgumentNullException(nameof(workDirectory));
        if (edit is null)
            throw new ArgumentNullException(nameof(edit));

        var manifest = Path.Combine(contentDirectory, ManifestFileName);
        if (!File.Exists(manifest))
            throw new FileNotFoundException("The base has no alldata.psb.m.", manifest);

        var packer = Packer();
        var extracted = Path.Combine(workDirectory, BaseName);
        try
        {
            Directory.CreateDirectory(extracted);
            AllDataPacker.UnpackFiles(manifest, extracted, packer, null);
            edit(extracted);
            AllDataPacker.Build(extracted, Path.Combine(contentDirectory, BaseName), packer, null);
            var plain = Path.Combine(contentDirectory, BaseName + ".psb");
            if (File.Exists(plain))
                File.Delete(plain);
        }
        finally
        {
            if (Directory.Exists(workDirectory))
                Directory.Delete(workDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Sets root.m2epi.brightness in a title_prof.psb.m to 1, which removes the darkening.
    /// </summary>
    /// <param name="titleProfilePath">The .psb.m file.</param>
    public static void SetFullBrightness(string titleProfilePath)
    {
        if (titleProfilePath is null)
            throw new ArgumentNullException(nameof(titleProfilePath));

        var packer = Packer();
        var plain = new MemoryStream();
        packer.DecompressFile(titleProfilePath, keepOrig: true, plain);
        plain.Position = 0;

        JToken root;
        ushort version;
        PsbFlags flags;
        using (var reader = new PsbReader(plain, null, null, false))
        {
            root = reader.Root;
            version = reader.Version;
            flags = reader.Flags;
        }
        var m2epi = root["m2epi"] as JObject ?? throw new InvalidDataException("title_prof.psb has no m2epi object.");
        m2epi["brightness"] = 1;

        var rewritten = new MemoryStream();
        new PsbWriter(root, null) { Version = version, Flags = flags }.Write(rewritten);
        rewritten.Position = 0;
        var plainPath = titleProfilePath.Substring(0, titleProfilePath.Length - 2);
        File.WriteAllBytes(plainPath, rewritten.ToArray());
        packer.CompressFile(plainPath, keepOrig: false, null);
    }

    /// <summary>
    /// Writes an entry, MDF-compressing it when its name ends in <see cref="CompressedSuffix"/>.
    /// </summary>
    /// <param name="entryPath">Where the entry sits in the unpacked archive.</param>
    /// <param name="data">Plain bytes.</param>
    public static void WriteEntry(string entryPath, byte[] data)
    {
        if (entryPath is null)
            throw new ArgumentNullException(nameof(entryPath));
        if (data is null)
            throw new ArgumentNullException(nameof(data));

        if (!entryPath.EndsWith(CompressedSuffix, StringComparison.OrdinalIgnoreCase))
        {
            File.WriteAllBytes(entryPath, data);
            return;
        }

        var plain = entryPath.Substring(0, entryPath.Length - CompressedSuffix.Length);
        File.WriteAllBytes(plain, data);
        if (File.Exists(entryPath))
            File.Delete(entryPath);
        Packer().CompressFile(plain, keepOrig: false, null);
    }

    private static MArchivePacker Packer() => new(new ZlibCodec(), Seed, KeyLength);
}
