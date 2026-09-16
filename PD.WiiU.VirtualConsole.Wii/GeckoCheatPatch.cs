using WiiSharp;

namespace PD.WiiU.VirtualConsole.Wii;

/// <summary>
/// Bakes Gecko cheat codes into a Wii game's main.dol: the code handler and the code list ride along as an extra text section, and the VI retrace handler branches into it every frame, as Gecko OS and USB Loader GX arrange at runtime.
/// </summary>
public static class GeckoCheatPatch
{
    /// <summary>
    /// Where the handler loads.
    /// </summary>
    public const uint HandlerAddress = 0x80001800;
    /// <summary>
    /// The handler's entry, which the hook branches to.
    /// </summary>
    public const uint HandlerEntry = 0x800018A8;
    /// <summary>
    /// Where the code list starts; the handler's own lis/ori pair names it.
    /// </summary>
    public const uint CodeListAddress = 0x800022A8;
    /// <summary>
    /// First address past the reserved area.
    /// </summary>
    public const uint CodeListEnd = 0x80003000;
    /// <summary>
    /// Code lines that fit between the list start and the end of the area, magic and terminator counted.
    /// </summary>
    public const int MaxLines = (int)((CodeListEnd - CodeListAddress) / 8) - 2;

    private const string HandlerResource = "codehandleronly.bin";
    private const uint Blr = 0x4E800020;
    private const int HookSearchBytes = 0x400;

    // the first instructions of the SDK's VI retrace handler; the branch replaces its final blr
    private static readonly byte[] ViRetraceHook = { 0x7C, 0xE3, 0x3B, 0x78, 0x38, 0x87, 0x00, 0x34, 0x38, 0xA7, 0x00, 0x38, 0x38, 0xC7, 0x00, 0x4C };

    /// <summary>
    /// Adds the handler and codes to a DOL and hooks it.
    /// </summary>
    /// <param name="dol">main.dol.</param>
    /// <param name="lines">Code lines.</param>
    /// <returns>The patched DOL.</returns>
    /// <exception cref="NotSupportedException">Too many code lines for the reserved area.</exception>
    /// <exception cref="InvalidDataException">The DOL has no VI retrace handler to hook, or the handler's area is already in use.</exception>
    public static byte[] Apply(byte[] dol, IReadOnlyList<GeckoCodeLine> lines)
    {
        if (dol is null)
            throw new ArgumentNullException(nameof(dol));
        if (lines is null)
            throw new ArgumentNullException(nameof(lines));
        if (lines.Count == 0)
            throw new ArgumentException("No code lines.", nameof(lines));
        if (lines.Count > MaxLines)
            throw new NotSupportedException($"{lines.Count} code lines; at most {MaxLines} fit in the handler's area.");

        var file = DolFile.Parse(dol);
        var hook = FindHook(file) ?? throw new InvalidDataException("The DOL has no VI retrace handler to hook; the game cannot take Gecko codes this way.");

        var handler = Handler();
        var section = new byte[CodeListAddress - HandlerAddress];
        Array.Copy(handler, section, Math.Min(handler.Length, section.Length));
        var gct = GeckoCodes.Build(lines);
        Array.Resize(ref section, section.Length + gct.Length);
        gct.CopyTo(section, CodeListAddress - HandlerAddress);
        try
        {
            file.AddSection(HandlerAddress, section, text: true);
        }
        catch (InvalidOperationException error)
        {
            throw new InvalidDataException("The DOL already uses the code handler's area.", error);
        }

        var branch = new byte[4];
        BigEndianWrite(branch, 0x48000000 | ((HandlerEntry - hook) & 0x03FFFFFC));
        file.Write(hook, branch);
        return file.ToBytes();
    }

    /// <summary>
    /// The embedded code handler.
    /// </summary>
    public static byte[] Handler()
    {
        using var stream = typeof(GeckoCheatPatch).Assembly.GetManifestResourceStream(HandlerResource)
            ?? throw new InvalidOperationException($"Embedded {HandlerResource} is missing.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    /// <summary>
    /// Address of the blr that ends the VI retrace handler, or null when the pattern is absent.
    /// </summary>
    private static uint? FindHook(DolFile file)
    {
        var start = file.Find(ViRetraceHook);
        if (start is null)
            return null;
        for (var at = start.Value; at < start.Value + HookSearchBytes; at += 4)
        {
            byte[] word;
            try
            {
                word = file.Read(at, 4);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
            if (BigEndianRead(word) == Blr)
                return at;
        }
        return null;
    }

    private static uint BigEndianRead(byte[] bytes) => (uint)(bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3]);

    private static void BigEndianWrite(byte[] bytes, uint value)
    {
        bytes[0] = (byte)(value >> 24);
        bytes[1] = (byte)(value >> 16);
        bytes[2] = (byte)(value >> 8);
        bytes[3] = (byte)value;
    }
}
