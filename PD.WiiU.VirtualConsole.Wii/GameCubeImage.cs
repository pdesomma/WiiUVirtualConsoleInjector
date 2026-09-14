using WiiSharp;

namespace PD.WiiU.VirtualConsole.Wii;

/// <summary>
/// Opens GameCube disc images as plain seekable streams.
/// </summary>
public static class GameCubeImage
{
    /// <summary>
    /// Opens a .iso or .gcm directly or decodes a .gcz on the fly; the returned disposable owns the container.
    /// </summary>
    /// <param name="path">Image path.</param>
    /// <param name="image">Seekable plain image.</param>
    /// <exception cref="NotSupportedException">Any other extension, or an NKit image.</exception>
    public static IDisposable Open(string path, out Stream image)
    {
        if (path is null)
            throw new ArgumentNullException(nameof(path));
        if (path.IndexOf(".nkit.", StringComparison.OrdinalIgnoreCase) >= 0)
            throw new NotSupportedException("NKit images are not supported; convert to a plain ISO or GCZ first.");

        var extension = Path.GetExtension(path);
        if (string.Equals(extension, ".iso", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".gcm", StringComparison.OrdinalIgnoreCase))
        {
            image = File.OpenRead(path);
            return image;
        }
        if (string.Equals(extension, ".gcz", StringComparison.OrdinalIgnoreCase))
        {
            var gcz = GczFile.Open(path);
            image = gcz.OpenStream();
            return gcz;
        }
        throw new NotSupportedException("Only .iso, .gcm and .gcz images are supported.");
    }

    /// <summary>
    /// Reads and checks the disc header.
    /// </summary>
    /// <param name="image">Seekable plain image.</param>
    /// <exception cref="InvalidDataException">Not a GameCube disc.</exception>
    public static DiscHeader ReadHeader(Stream image)
    {
        if (image is null)
            throw new ArgumentNullException(nameof(image));

        var bytes = new byte[DiscHeader.Size];
        image.Position = 0;
        var read = 0;
        while (read < bytes.Length)
        {
            var n = image.Read(bytes, read, bytes.Length - read);
            if (n == 0)
                throw new InvalidDataException("Image is shorter than a disc header.");
            read += n;
        }
        var header = DiscHeader.Parse(bytes);
        if (!header.IsGameCube)
            throw new InvalidDataException("Not a GameCube disc image.");
        return header;
    }
}
