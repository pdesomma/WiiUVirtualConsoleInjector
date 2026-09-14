using WiiUSharp.Rpx;

namespace PD.WiiU.VirtualConsole.Retro;

/// <summary>
/// Rewrites the two display-size instructions of a NES or SNES executable for 8:7 pixel-perfect or 16:9 output.
/// </summary>
public static class AspectRatioPatch
{
    private static readonly byte[] Pad = { 0x39, 0x20, 0x03, 0x56, 0x38, 0x60 };
    private static readonly byte[] PadNes = { 0x39, 0x20, 0x04, 0x29, 0x38, 0x60 };
    private static readonly byte[] PadSnes = { 0x39, 0x20, 0x03, 0xE4, 0x38, 0x60 };
    private static readonly byte[] PadWide = { 0x39, 0x20, 0x02, 0x80, 0x38, 0x60 };
    private static readonly byte[] Tv = { 0x04, 0x38, 0x38, 0xE0, 0x07, 0x80, 0x90 };
    private static readonly byte[] TvNes = { 0x04, 0x38, 0x38, 0xE0, 0x09, 0x5F, 0x90 };
    private static readonly byte[] TvSnes = { 0x04, 0x38, 0x38, 0xE0, 0x08, 0xC0, 0x90 };
    private static readonly byte[] TvWide = { 0x04, 0x38, 0x38, 0xE0, 0x05, 0xA0, 0x90 };

    /// <summary>
    /// Applies the patch in place; the TV pattern is the first match in the file, the GamePad pattern the last.
    /// </summary>
    /// <param name="rpx">Executable with sections plain.</param>
    /// <param name="ratio">Target ratio.</param>
    /// <param name="nes">True for a NES executable; ignored for <see cref="AspectRatio.Wide"/>.</param>
    /// <returns>True when both patterns were found and rewritten.</returns>
    public static bool Apply(RpxFile rpx, AspectRatio ratio, bool nes)
    {
        if (rpx is null)
            throw new ArgumentNullException(nameof(rpx));

        var sections = rpx.Sections.Where(s => s.HasData).OrderBy(s => s.StoredOffset).ToArray();
        var tv = sections.Select(s => (Section: s, At: IndexOf(s.Data, Tv, last: false))).FirstOrDefault(m => m.At >= 0);
        var pad = sections.Reverse().Select(s => (Section: s, At: IndexOf(s.Data, Pad, last: true))).FirstOrDefault(m => m.At >= 0);
        if (tv.Section is null || pad.Section is null)
            return false;

        var (newTv, newPad) = ratio switch
        {
            AspectRatio.Wide => (TvWide, PadWide),
            AspectRatio.PixelPerfect => nes ? (TvNes, PadNes) : (TvSnes, PadSnes),
            _ => throw new ArgumentOutOfRangeException(nameof(ratio)),
        };
        Replace(tv.Section, tv.At, newTv);
        Replace(pad.Section, pad.At, newPad);
        return true;
    }

    private static int IndexOf(byte[] data, byte[] pattern, bool last)
    {
        var found = -1;
        for (var i = 0; i + pattern.Length <= data.Length; i++)
        {
            var match = true;
            for (var j = 0; j < pattern.Length && match; j++)
                match = data[i + j] == pattern[j];
            if (!match)
                continue;
            found = i;
            if (!last)
                break;
        }
        return found;
    }

    private static void Replace(Section section, int at, byte[] bytes)
    {
        var data = (byte[])section.Data.Clone();
        bytes.CopyTo(data, at);
        section.Data = data;
    }
}
