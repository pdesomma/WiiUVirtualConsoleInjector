using GMWare.M2.MArchive;
using GMWare.M2.Psb;
using Newtonsoft.Json.Linq;

namespace PD.WiiU.VirtualConsole.Gba.Tests;

/// <summary>
/// Builds and reads a GBA-base-shaped alldata archive with the same library the injector uses.
/// </summary>
internal static class FakeAllData
{
    public const string RomName = "rom/AGB-BASE.gba";

    public static byte[] BaseRom => Enumerable.Range(0, 0x400).Select(i => (byte)(i * 3)).ToArray();

    public static void Build(string contentDirectory, string scratch, int brightness = 0)
    {
        var packer = Packer();
        var root = Path.Combine(scratch, "src");
        Directory.CreateDirectory(Path.Combine(root, "rom"));
        Directory.CreateDirectory(Path.Combine(root, "config"));
        File.WriteAllBytes(Path.Combine(root, RomName.Replace('/', Path.DirectorySeparatorChar)), BaseRom);
        File.WriteAllText(Path.Combine(root, "config", "readme.txt"), "keep me");

        var profile = new JObject
        {
            ["m2epi"] = new JObject { ["brightness"] = brightness, ["contrast"] = 5 },
            ["title"] = "Fake",
        };
        var plain = Path.Combine(root, "config", "title_prof.psb");
        using (var stream = File.Create(plain))
            new PsbWriter(profile, null) { Version = 2 }.Write(stream);
        packer.CompressFile(plain, keepOrig: false, null);

        Directory.CreateDirectory(contentDirectory);
        AllDataPacker.Build(root, Path.Combine(contentDirectory, "alldata"), packer, null);
        Directory.Delete(root, recursive: true);
    }

    public static (byte[] Rom, int Brightness, string[] Files) Read(string contentDirectory, string scratch)
    {
        var packer = Packer();
        var extracted = Path.Combine(scratch, "read");
        Directory.CreateDirectory(extracted);
        AllDataPacker.UnpackFiles(Path.Combine(contentDirectory, "alldata.psb.m"), extracted, packer, null);

        var rom = File.ReadAllBytes(Directory.GetFiles(extracted, "*.gba", SearchOption.AllDirectories).Single());
        var profilePath = Directory.GetFiles(extracted, "title_prof.psb.m", SearchOption.AllDirectories).Single();
        var plain = new MemoryStream();
        packer.DecompressFile(profilePath, keepOrig: true, plain);
        plain.Position = 0;
        using var reader = new PsbReader(plain, null, null, false);
        var brightness = (int)reader.Root["m2epi"]!["brightness"]!;
        var files = Directory.GetFiles(extracted, "*", SearchOption.AllDirectories)
            .Select(f => f.Substring(extracted.Length + 1).Replace(Path.DirectorySeparatorChar, '/'))
            .OrderBy(f => f, StringComparer.Ordinal).ToArray();
        return (rom, brightness, files);
    }

    private static MArchivePacker Packer() => new(new ZlibCodec(), AllDataArchive.Seed, AllDataArchive.KeyLength);
}
