namespace WiiUVirtualConsoleInjector.ViewModels;

/// <summary>
/// Validity of the hex a user has typed for a key, before parsing.
/// </summary>
internal static class HexKeyText
{
    /// <summary>
    /// True when the trimmed text is exactly the hex digits of a key of this many bytes.
    /// </summary>
    /// <param name="text">What the user typed.</param>
    /// <param name="size">Bytes the key holds.</param>
    public static bool IsValid(string? text, int size)
    {
        if (text is null)
            return false;

        var hex = text.AsSpan().Trim();
        return hex.Length == size * 2 && !hex.ContainsAnyExcept("0123456789abcdefABCDEF");
    }
}
