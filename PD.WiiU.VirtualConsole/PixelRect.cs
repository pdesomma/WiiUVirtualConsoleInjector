namespace PD.WiiU.VirtualConsole;

/// <summary>
/// A rectangle in image pixels.
/// </summary>
/// <param name="X">Left edge.</param>
/// <param name="Y">Top edge.</param>
/// <param name="Width">Width.</param>
/// <param name="Height">Height.</param>
public readonly record struct PixelRect(int X, int Y, int Width, int Height)
{
    /// <summary>
    /// Bottom edge.
    /// </summary>
    public int Bottom => Y + Height;
    /// <summary>
    /// Right edge.
    /// </summary>
    public int Right => X + Width;
}
