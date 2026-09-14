using System.Text;

namespace PD.WiiU.VirtualConsole.Retro;

/// <summary>
/// Rewrites the frame and dark-filter panes in the N64 base's content/FrameLayout.arc.
/// </summary>
public static class FrameLayoutPatch
{
    /// <summary>
    /// File the panes live in.
    /// </summary>
    public const string FileName = "FrameLayout.arc";

    private const uint One = 0x3F800000;
    private const uint StandardWidth = 0x44B40000;
    private const uint WideWidth = 0x44F00000;

    /// <summary>
    /// Sets the frame pane to full height at 4:3 or 16:9 and shows or hides the dark-filter mask, in place.
    /// </summary>
    /// <param name="archive">FrameLayout.arc bytes.</param>
    /// <param name="wideScreen">Stretch the frame to 16:9.</param>
    /// <param name="removeDarkFilter">Hide the darkening mask.</param>
    /// <exception cref="InvalidDataException">Not the SARC/FLYT layout the patch knows.</exception>
    public static void Apply(byte[] archive, bool wideScreen, bool removeDarkFilter)
    {
        if (archive is null)
            throw new ArgumentNullException(nameof(archive));

        Require(archive, 0, 0x40);
        if (!HasTag(archive, 0, "SARC") || ReadUInt16(archive, 6) != 0xFEFF)
            throw new InvalidDataException("FrameLayout.arc is not a big-endian SARC archive.");

        var layout = ReadInt32(archive, 0x0C) + ReadInt32(archive, 0x38);
        Require(archive, layout, 0x14);
        if (!HasTag(archive, layout, "FLYT") || ReadUInt16(archive, layout + 4) != 0xFEFF)
            throw new InvalidDataException("FrameLayout.arc does not hold a FLYT layout where expected.");

        var headerSize = ReadUInt16(archive, layout + 6);
        var layoutSize = ReadInt32(archive, layout + 0x0C);
        Require(archive, layout, layoutSize);
        if (headerSize < 0x14 || headerSize > layoutSize)
            throw new InvalidDataException("FLYT header size is out of range.");

        var frame = -1;
        var mask = -1;
        var end = layout + layoutSize;
        for (var at = layout + headerSize; at < end && (frame < 0 || mask < 0); )
        {
            if (end - at < 8)
                throw new InvalidDataException("FLYT section header is truncated.");
            var size = ReadInt32(archive, at + 4);
            if (size < 8 || size > end - at)
                throw new InvalidDataException("FLYT section size is out of range.");

            if (HasTag(archive, at, "pic1"))
            {
                if (size < 0x24)
                    throw new InvalidDataException("FLYT picture pane is truncated.");
                var name = PaneName(archive, at + 0x0C);
                if (name == "frame")
                {
                    if (size < 0x50)
                        throw new InvalidDataException("The frame pane is too short to patch.");
                    frame = at;
                }
                else if (name == "frame_mask")
                {
                    mask = at;
                }
            }
            at += size;
        }
        if (frame < 0 || mask < 0)
            throw new InvalidDataException("The layout has no frame or frame_mask pane.");

        WriteUInt32(archive, frame + 0x2C, 0);
        WriteUInt32(archive, frame + 0x30, 0);
        WriteUInt32(archive, frame + 0x44, One);
        WriteUInt32(archive, frame + 0x48, One);
        WriteUInt32(archive, frame + 0x4C, wideScreen ? WideWidth : StandardWidth);
        archive[mask + 0x08] = removeDarkFilter ? (byte)0 : (byte)1;
    }

    private static bool HasTag(byte[] data, int at, string tag)
    {
        for (var i = 0; i < tag.Length; i++)
            if (data[at + i] != tag[i])
                return false;
        return true;
    }

    private static string PaneName(byte[] data, int at)
    {
        var end = Array.IndexOf(data, (byte)0, at, 0x18);
        return Encoding.ASCII.GetString(data, at, (end < 0 ? at + 0x18 : end) - at);
    }

    private static int ReadInt32(byte[] data, int at)
    {
        Require(data, at, 4);
        var value = (uint)data[at] << 24 | (uint)data[at + 1] << 16 | (uint)data[at + 2] << 8 | data[at + 3];
        if (value > int.MaxValue)
            throw new InvalidDataException("FrameLayout.arc holds an offset that is too large.");
        return (int)value;
    }

    private static int ReadUInt16(byte[] data, int at)
    {
        Require(data, at, 2);
        return data[at] << 8 | data[at + 1];
    }

    private static void Require(byte[] data, int at, int length)
    {
        if (at < 0 || length < 0 || at > data.Length || length > data.Length - at)
            throw new InvalidDataException("FrameLayout.arc is truncated.");
    }

    private static void WriteUInt32(byte[] data, int at, uint value)
    {
        data[at] = (byte)(value >> 24);
        data[at + 1] = (byte)(value >> 16);
        data[at + 2] = (byte)(value >> 8);
        data[at + 3] = (byte)value;
    }
}
