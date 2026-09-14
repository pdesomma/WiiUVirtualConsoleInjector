namespace PD.WiiU.VirtualConsole.Wii;

/// <summary>
/// A pattern to find and the bytes to write relative to each match.
/// </summary>
internal sealed class BytePatch
{
    public BytePatch(byte?[] pattern, params (int Offset, byte[] Bytes)[] writes)
    {
        Pattern = pattern;
        Writes = writes;
    }

    /// <summary>
    /// Bytes to match; null matches anything.
    /// </summary>
    public byte?[] Pattern { get; }
    /// <summary>
    /// Writes relative to the match offset.
    /// </summary>
    public IReadOnlyList<(int Offset, byte[] Bytes)> Writes { get; }

    public static BytePatch At(byte[] pattern, int offset, params byte[] bytes) =>
        new(pattern.Select(b => (byte?)b).ToArray(), (offset, bytes));

    public static BytePatch Replace(byte[] pattern, params byte[] bytes) => At(pattern, 0, bytes);

    public int Apply(byte[] image)
    {
        var matches = new List<int>();
        for (var offset = 0; offset + Pattern.Length <= image.Length; offset++)
            if (MatchesAt(image, offset))
                matches.Add(offset);

        var applied = 0;
        foreach (var match in matches)
        {
            if (!Fits(image, match))
                continue;
            foreach (var (offset, bytes) in Writes)
                Array.Copy(bytes, 0, image, match + offset, bytes.Length);
            applied++;
        }
        return applied;
    }

    private bool Fits(byte[] image, int match) =>
        Writes.All(w => match + w.Offset >= 0 && match + w.Offset + w.Bytes.Length <= image.Length);

    private bool MatchesAt(byte[] image, int offset)
    {
        for (var i = 0; i < Pattern.Length; i++)
            if (Pattern[i] is { } expected && image[offset + i] != expected)
                return false;
        return true;
    }
}
