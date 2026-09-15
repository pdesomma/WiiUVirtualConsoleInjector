using SkiaSharp;
using TargaSharp;
using TargaSharp.IO;

namespace PD.WiiU.VirtualConsole.Infrastructure;

/// <summary>
/// Turns a title's TGA into a PNG the UI can show.
/// </summary>
public static class TgaPng
{
    /// <summary>
    /// Encodes a 24 or 32 bpp uncompressed true-color TGA as PNG.
    /// </summary>
    /// <param name="tga">TGA file bytes.</param>
    /// <exception cref="NotSupportedException">Color-mapped, grayscale, RLE or an unusual depth.</exception>
    /// <exception cref="InvalidDataException">Not a TGA.</exception>
    public static byte[] Encode(byte[] tga)
    {
        if (tga is null)
            throw new ArgumentNullException(nameof(tga));

        TgaFile file;
        try
        {
            file = new TgaFile(tga);
        }
        catch (Exception e) when (e is TgaFormatException or ArgumentException or EndOfStreamException or InvalidOperationException)
        {
            throw new InvalidDataException("Not a TGA image.", e);
        }

        var depth = (int)file.Header.ImageSpec.PixelDepth;
        if (file.Header.ImageType != TgaImageType.UncompressedTrueColor || depth is not (24 or 32))
            throw new NotSupportedException("Only 24 and 32 bpp uncompressed true-color TGAs are supported.");
        var data = file.ImageArea.ImageData ?? throw new InvalidDataException("TGA has no image data.");

        int width = file.Width, height = file.Height, bytesPerPixel = depth / 8;
        var origin = file.Header.ImageSpec.ImageDescriptor.ImageOrigin;
        var bottomUp = origin is TgaImageOrigin.BottomLeft or TgaImageOrigin.BottomRight;
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            var sourceRow = bottomUp ? height - 1 - y : y;
            for (var x = 0; x < width; x++)
            {
                var s = (sourceRow * width + x) * bytesPerPixel;
                var t = (y * width + x) * 4;
                pixels[t] = data[s];
                pixels[t + 1] = data[s + 1];
                pixels[t + 2] = data[s + 2];
                pixels[t + 3] = bytesPerPixel == 4 ? data[s + 3] : (byte)0xFF;
            }
        }

        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul));
        System.Runtime.InteropServices.Marshal.Copy(pixels, 0, bitmap.GetPixels(), pixels.Length);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }
}
