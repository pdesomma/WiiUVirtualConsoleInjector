using WiiUSharp;
using WiiUSharp.Nus;

namespace PD.WiiU.VirtualConsole.Tests;

/// <summary>
/// Writes a package folder shaped like an installer takes it: a real TMD over made-up contents.
/// </summary>
internal static class FakePackage
{
    public static readonly TitleId Id = TitleId.Parse("0005000010180600");

    /// <summary>
    /// Writes a complete package with one plain and one hashed content.
    /// </summary>
    public static string Write(string folder, ushort version = 0)
    {
        Directory.CreateDirectory(folder);
        var contents = new[]
        {
            new ContentRecord(0, ContentType.Content | ContentType.Encrypted, 0x10000, new byte[20], 0),
            new ContentRecord(1, ContentType.Content | ContentType.Encrypted | ContentType.Hashed, 0x30000, new byte[20], 3),
        };
        File.WriteAllBytes(Path.Combine(folder, "title.tmd"), Tmd.Build(new TitleInfo(Id, 0, version), contents));
        File.WriteAllBytes(Path.Combine(folder, "title.tik"), new byte[848]);
        File.WriteAllBytes(Path.Combine(folder, "title.cert"), new byte[2560]);
        File.WriteAllBytes(Path.Combine(folder, "00000000.app"), new byte[0x10000]);
        File.WriteAllBytes(Path.Combine(folder, "00000003.app"), new byte[0x30000]);
        File.WriteAllBytes(Path.Combine(folder, "00000003.h3"), new byte[20]);
        return folder;
    }
}
