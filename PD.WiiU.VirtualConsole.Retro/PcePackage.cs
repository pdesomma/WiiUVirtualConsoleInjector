using System.Text;

namespace PD.WiiU.VirtualConsole.Retro;

/// <summary>
/// Builds the pce.pkg the TurboGrafx-16 emulator loads: a little-endian length, then {length, name, NUL, data} entries.
/// </summary>
public static class PcePackage
{
    /// <summary>
    /// Name of the first entry.
    /// </summary>
    public const string ConfigName = "pceconfig.bin";
    /// <summary>
    /// Bytes in the config entry.
    /// </summary>
    public const int ConfigSize = 0xA0;
    /// <summary>
    /// File the emulator reads.
    /// </summary>
    public const string FileName = "pce.pkg";
    /// <summary>
    /// Longest name the config entry can hold.
    /// </summary>
    public const int MaxNameLength = 64;
    /// <summary>
    /// Folder name TurboCD entries are stored under, as the original tool did.
    /// </summary>
    public const string TurboCdFolder = "test";

    private static readonly byte[] TurboCdConfigPrefix =
    {
        0x01, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    };

    /// <summary>
    /// Package for a HuCard ROM: config, then the ROM stored twice.
    /// </summary>
    /// <param name="romName">File name the config lists.</param>
    /// <param name="rom">ROM bytes.</param>
    /// <exception cref="ArgumentException">Name longer than <see cref="MaxNameLength"/>.</exception>
    public static byte[] BuildHuCard(string romName, byte[] rom)
    {
        if (rom is null)
            throw new ArgumentNullException(nameof(rom));
        var name = NameBytes(romName);

        var body = new MemoryStream();
        WriteEntry(body, Encoding.ASCII.GetBytes(ConfigName), Config(new byte[32], name));
        WriteEntry(body, name, rom);
        WriteEntry(body, name, rom);
        return Wrap(body);
    }

    /// <summary>
    /// Package for a TurboCD game: config, the .hcd, then every file the .hcd lists that exists.
    /// </summary>
    /// <param name="directory">Folder holding exactly one .hcd plus its .ogg and .bin files.</param>
    /// <exception cref="FileNotFoundException">No .hcd, .ogg or .bin file.</exception>
    /// <exception cref="InvalidDataException">More than one .hcd.</exception>
    public static byte[] BuildTurboCd(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Directory is required.", nameof(directory));
        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException(directory);

        var hcds = Directory.GetFiles(directory, "*.hcd");
        if (hcds.Length == 0 || Directory.GetFiles(directory, "*.ogg").Length == 0 || Directory.GetFiles(directory, "*.bin").Length == 0)
            throw new FileNotFoundException("A TurboCD folder needs one .hcd and at least one .ogg and one .bin file.");
        if (hcds.Length > 1)
            throw new InvalidDataException("A TurboCD folder must hold exactly one .hcd file.");

        var hcd = hcds[0];
        var hcdName = NameBytes(TurboCdFolder + "/" + Path.GetFileName(hcd));
        var body = new MemoryStream();
        WriteEntry(body, Encoding.ASCII.GetBytes(ConfigName), Config(TurboCdConfigPrefix, hcdName));
        WriteEntry(body, hcdName, File.ReadAllBytes(hcd));
        foreach (var listed in ListedFiles(hcd))
        {
            var path = Path.Combine(directory, listed);
            if (File.Exists(path))
                WriteEntry(body, Encoding.ASCII.GetBytes(TurboCdFolder + "/" + listed), File.ReadAllBytes(path));
        }
        return Wrap(body);
    }

    /// <summary>
    /// File names in the third column of each .hcd line.
    /// </summary>
    /// <param name="hcdPath">The .hcd file.</param>
    public static IReadOnlyList<string> ListedFiles(string hcdPath)
    {
        if (hcdPath is null)
            throw new ArgumentNullException(nameof(hcdPath));

        return File.ReadAllLines(hcdPath)
            .Select(line => line.Split(','))
            .Where(columns => columns.Length > 2)
            .Select(columns => columns[2].Trim())
            .Where(name => name.Length > 0)
            .ToArray();
    }

    private static byte[] Config(byte[] prefix, byte[] name)
    {
        var config = new byte[ConfigSize];
        prefix.CopyTo(config, 0);
        name.CopyTo(config, 32);
        name.CopyTo(config, 32 + MaxNameLength);
        return config;
    }

    private static byte[] NameBytes(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Name is required.", nameof(name));
        var bytes = Encoding.ASCII.GetBytes(name);
        if (bytes.Length > MaxNameLength)
            throw new ArgumentException($"Name must be at most {MaxNameLength} characters: {name}", nameof(name));
        return bytes;
    }

    private static byte[] Wrap(MemoryStream body)
    {
        var package = new byte[4 + body.Length];
        WriteLength(package, 0, (int)body.Length);
        body.ToArray().CopyTo(package, 4);
        return package;
    }

    private static void WriteEntry(Stream output, byte[] name, byte[] data)
    {
        var length = new byte[4];
        WriteLength(length, 0, data.Length);
        output.Write(length, 0, 4);
        output.Write(name, 0, name.Length);
        output.WriteByte(0);
        output.Write(data, 0, data.Length);
    }

    private static void WriteLength(byte[] bytes, int at, int value)
    {
        bytes[at] = (byte)value;
        bytes[at + 1] = (byte)(value >> 8);
        bytes[at + 2] = (byte)(value >> 16);
        bytes[at + 3] = (byte)(value >> 24);
    }
}
